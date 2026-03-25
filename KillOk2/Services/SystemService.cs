using HelperExtensions;
using HelperMethods;
using KillOk2.ViewModels;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Imaging;

namespace KillOk2.Services;

public static class SystemService
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    #region Imports

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDlgItem(IntPtr hDlg, int nIdDlgItem);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    #endregion

    private static readonly IReadOnlySet<string> CriticalProcesses = new HashSet<string> { "explorer", "shellexperiencehost", "searchhost" };
    private const string DialogClass = "#32770";
    private static readonly Dictionary<IntPtr, DateTime> DialogsToIgnore = [];
    private static readonly IntPtr ErrorIcon = LoadIcon(IntPtr.Zero, 0x7F01);
    private const string ExplorerClass = "WorkerW";
    private const int GwlStyle = -16;
    private const int IdIcon = 0x14;
    private static readonly IntPtr InformationIcon = LoadIcon(IntPtr.Zero, 0x7F04);
    private static readonly TimeSpan IgnoreTime = TimeSpan.FromMinutes(5);
    private const string OpenSaveDialogClass = "DirectUIHWND";
    private static readonly IntPtr QuestionIcon = LoadIcon(IntPtr.Zero, 0x7F02);
    private const string StaticClass = "Static";
    private const uint StmGetIcon = 0x0170;
    private const string ToolbarClass = "ToolbarWindow32";
    private static readonly IntPtr WarningIcon = LoadIcon(IntPtr.Zero, 0x7F03);
    private const uint WmClose = 0x0010;
    private const uint WsThickFrame = 0x00040000;

    #region Public Methods

    #region CloseDialogs
    /// <summary>
    /// Closes all the eligible open dialogs.
    /// </summary>
    /// <remarks>This method will attempt to close all dialog types, except Questions.</remarks>
    /// <returns>A list of the closed dialog data.</returns>
    public static IReadOnlyCollection<DialogInfoViewModel> CloseDialogs()
    {
        List<DialogInfoViewModel> closedDialogs = [];

        DateTime currentTime = DateTime.Now;
        DialogsToIgnore.Where(_ => currentTime - _.Value > IgnoreTime).ToList().ForEach(_ => DialogsToIgnore.Remove(_.Key));

        StringBuilder dialogClass = new(512);
        EnumWindows((hWnd, _) =>
            {
                if (DialogsToIgnore.ContainsKey(hWnd))
                {
#if DEBUG
                    Debug.WriteLine($"{DateTime.Now:HH:mm:ss} - Dialog ignored: ignore list.");
#endif
                    return true;
                }

                int ownerProcessId = 0;
                string? ownerProcessName = null;

                try
                {
                    dialogClass.Clear();
                    if (GetClassName(hWnd, dialogClass, dialogClass.Capacity) <= 0
                        || dialogClass.ToString() != DialogClass)
                    {
                        RegisterIgnoredDialog(hWnd, "Dialog ignored: Not a dialog.");
                        return true;
                    }

                    if (!IsWindowVisible(hWnd))
                    {
                        RegisterIgnoredDialog(hWnd, "Dialog ignored: Not a visible window/dialog.");
                        return true;
                    }

                    if (IsOpenSaveDialog(hWnd))
                    {
                        RegisterIgnoredDialog(hWnd, "Dialog ignored: Open/Save dialog.");
                        return true;
                    }

                    DialogType dialogType = GetDialogType(hWnd);
                    if (dialogType == DialogType.Question)
                    {
                        RegisterIgnoredDialog(hWnd, "Dialog ignored: Question dialog.");
                        return true;
                    }

                    (ownerProcessId, ownerProcessName, BitmapSource ownerProcessIcon) = GetOwnerProcessData(hWnd);
                    if (ownerProcessName == null || CriticalProcesses.Contains(ownerProcessName))
                    {
                        RegisterIgnoredDialog(hWnd, "Dialog ignored: Critical process.");
                        return true;
                    }

                    StringBuilder dialogTitle = new(256);
                    GetWindowText(hWnd, dialogTitle, dialogTitle.Capacity);
                    if (dialogTitle.Length == 0)
                    {
                        RegisterIgnoredDialog(hWnd, "Dialog ignored: No title.");
                        return true;
                    }

                    string dialogMessage = GetDialogMessage(hWnd);
                    if (dialogMessage.IsNullOrEmpty())
                    {
                        RegisterIgnoredDialog(hWnd, "Dialog ignored: No message.");
                        return true;
                    }

                    PostMessage(hWnd, WmClose, IntPtr.Zero, IntPtr.Zero);

                    closedDialogs.Add(
                        new(
                            dialogType,
                            dialogTitle.ToString(),
                            dialogMessage,
                            ownerProcessId,
                            ownerProcessName,
                            ownerProcessIcon,
                            DateTime.Now));
                }
                catch (Exception ex)
                {
                    RegisterIgnoredDialog(hWnd, $"Dialog ignored: {ownerProcessName} ({ownerProcessId}) -> {ex.Message}");
                }

                return true;
            },
            IntPtr.Zero);

        return closedDialogs;
    }
    #endregion

    #endregion

    #region Private Methods

    #region GetDialogMessage
    /// <summary>
    /// Retrieves the message text of a given dialog.
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns>The message text for the specified dialog.</returns>
    private static string GetDialogMessage(IntPtr hWnd)
    {
        StringBuilder message = new(512);
        StringBuilder messageBuffer = new(256);

        IntPtr hText = FindWindowEx(hWnd, IntPtr.Zero, StaticClass, null);
        while (hText != IntPtr.Zero)
        {
            messageBuffer.Clear();

            if (GetWindowText(hText, messageBuffer, messageBuffer.Capacity) > 0)
                message.Append(messageBuffer, " ");

            hText = FindWindowEx(hWnd, hText, StaticClass, null);
        }

        return message.ToString();
    }
    #endregion

    #region GetDialogType
    /// <summary>
    /// Retrieves the dialog type according to its icon.
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns>The specified dialog type.</returns>
    private static DialogType GetDialogType(IntPtr hWnd)
    {
        IntPtr hIconControl = GetDlgItem(hWnd, IdIcon);

        if (hIconControl == IntPtr.Zero)
            return DialogType.Other;

        IntPtr hIcon = SendMessage(hIconControl, StmGetIcon, IntPtr.Zero, IntPtr.Zero);

        return hIcon switch
        {
            _ when hIcon == InformationIcon => DialogType.Information,
            _ when hIcon == WarningIcon => DialogType.Warning,
            _ when hIcon == ErrorIcon => DialogType.Error,
            _ when hIcon == QuestionIcon => DialogType.Question,
            _ => DialogType.Other,
        };
    }
    #endregion

    #region GetOwnerProcessData
    /// <summary>
    /// Retrieves the info of a given dialog owner process.
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns>The info of the process that owns the given dialog.</returns>
    private static (int processId, string? processName, BitmapSource processIcon) GetOwnerProcessData(IntPtr hWnd)
    {
        GetWindowThreadProcessId(hWnd, out uint pid);

        int processId = (int)pid;
        using Process? process = SystemHelper.GetProcessById(processId);

        return (processId, process?.ProcessName, SystemHelper.GetIcon(process?.MainModule?.FileName!, false));
    }
    #endregion

    #region IsOpenSaveDialog
    /// <summary>
    /// Determines whether the specified dialog is an Open/Save dialog.
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns><see langword="true"/>, if the specified dialog is an Open/Save dialog; false, otherwise.</returns>
    private static bool IsOpenSaveDialog(IntPtr hWnd) =>
        FindWindowEx(hWnd, IntPtr.Zero, OpenSaveDialogClass, null) != IntPtr.Zero // Checks if the dialog is a legacy Open/Save dialog.
    || (GetWindowLong(hWnd, GwlStyle) & WsThickFrame) != 0 // Checks if the dialog is resizable.
    || FindWindowEx(hWnd, IntPtr.Zero, ExplorerClass, null) != IntPtr.Zero // Checks if the dialog contains an Explorer control.
    || FindWindowEx(hWnd, IntPtr.Zero, ToolbarClass, null) != IntPtr.Zero; // Checks if the dialog contains a toolbar control.
    #endregion

    #region RegisterIgnoredDialog
    /// <summary>
    /// Registers the specified dialog.
    /// </summary>
    /// <param name="hWnd"></param>
    /// <param name="reason"></param>
    private static void RegisterIgnoredDialog(IntPtr hWnd, string reason)
    {
        DialogsToIgnore[hWnd] = DateTime.Now;
#if DEBUG
        Debug.WriteLine($"{DateTime.Now:HH:mm:ss} - Dialog ignored: {reason}");
#endif
    }
    #endregion

    #endregion
}