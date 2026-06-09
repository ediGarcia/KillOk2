using HelperExtensions;
using HelperMethods;
using KillOk2.Models;
using System.Runtime.InteropServices;
using System.Text;

namespace KillOk2.Services;

public class SystemService : ISystemService
{
    #region Imports

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // LR_SHARED ensures the same HICON handle is returned for the same icon each call,
    // making handle-equality comparisons reliable.
    [DllImport("user32.dll")]
    private static extern IntPtr LoadImage(IntPtr hInst, IntPtr name, uint type, int cx, int cy, uint fuLoad);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        public int Bottom { get; set; }

        public int Left { get; set; }

        public int Right { get; set; }

        public int Top { get; set; }
        // ReSharper restore UnusedAutoPropertyAccessor.Local

        public bool HasPositiveSize =>
            Right > Left && Bottom > Top;
    }

    // Window class for all dialog boxes (MessageBox, TaskDialog, etc.)
    private const string DialogClass = "#32770";
    private const string StaticClass = "Static";

    private const uint WmClose = 0x0010; // WM_CLOSE
    private const uint StmGetImage = 0x0173; //STM_GETIMAGE
    private const uint ImageIcon = 1; //IMAGE_ICON
    private const uint LrShared = 0x8000; //LR_SHARED

    private const int GwlStyle = -16; //GWL_STYLE
    private const int SsTypeMask = 0x1F; // SS_TYPEMASK
    private const int SsIcon = 0x3;   // SS_ICON: Static control displays an icon

    // Standard system icon resource IDs
    private const int ErrorId = 32513; // IDI_ERROR
    private const int InformationId = 32516; // IDI_INFORMATION
    private const int QuestionId = 32514; // IDI_QUESTION
    private const int WarningId = 32515; // IDI_WARNING

    // Cached shared icon handles — constant within a Windows session
    private readonly IntPtr _hError = LoadImage(IntPtr.Zero, new(ErrorId), ImageIcon, 0, 0, LrShared);
    private readonly IntPtr _hInformation = LoadImage(IntPtr.Zero, new(InformationId), ImageIcon, 0, 0, LrShared);
    private readonly IntPtr _hQuestion = LoadImage(IntPtr.Zero, new(QuestionId), ImageIcon, 0, 0, LrShared);
    private readonly IntPtr _hWarning = LoadImage(IntPtr.Zero, new(WarningId), ImageIcon, 0, 0, LrShared);

    #endregion

    private static readonly string PersistedDataPath = PathHelper.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Kill OK",
        "data.json");

    #region Public Methods

    #region CloseDialogs
    /// <inheritdoc />
    public IEnumerable<DialogInfo> CloseDialogs(IEnumerable<ProcessFilter> filters)
    {
        List<DialogInfo> closedDialogs = [];
        Dictionary<int, (ProcessFilter, string)> processes = [];

        filters.ForEach(_ => SystemHelper.GetProcesses(_.ProcessSearchPattern).ForEach(__ => processes[__.Id] = (_, __.ProcessName)));

        EnumWindows(
            (hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out uint pid);
                int processId = (int)pid;
                if (!processes.TryGetValue(processId, out (ProcessFilter FilterData, string ProcessName) filter))
                    return true;

                if (!IsDialogVisible(hWnd))
                    return true;

                (DialogType type, string title, string message) = CollectDialogDetails(hWnd);

                if (!IsEligibleType(filter.FilterData, type))
                    return true;

                closedDialogs.Add(new(
                    type,
                    title,
                    message,
                    filter.ProcessName,
                    processId,
                    DateTime.Now
                ));

                PostMessage(hWnd, WmClose, IntPtr.Zero, IntPtr.Zero);
                return true;
            },
            IntPtr.Zero);

        return closedDialogs;
    }
    #endregion

    #region LoadPersistedFilters
    /// <inheritdoc />
    public IEnumerable<ProcessFilter>? LoadPersistedFilters() =>
        FileHelper.Exists(PersistedDataPath)
            ? FileHelper.ReadJson<IEnumerable<ProcessFilter>>(PersistedDataPath)
            : null;
    #endregion

    #region PersistFilters
    /// <inheritdoc />
    public void PersistFilters(IEnumerable<ProcessFilter> filters) =>
        FileHelper.WriteJson(PersistedDataPath, filters);
    #endregion

    #endregion

    #region Private Methods

    #region CollectDialogDetails
    /// <summary>
    /// Collects the dialog's type, title, and message text by enumerating its child controls.
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns></returns>
    private (DialogType Type, string Title, string Message) CollectDialogDetails(IntPtr hWnd)
    {
        DialogType type = DialogType.Other;
        StringBuilder message = new();

        EnumChildWindows(
            hWnd,
            (child, _) =>
            {
                StringBuilder cls = new(32);
                GetClassName(child, cls, 32);
                if (!cls.ToString().Equals(StaticClass, StringComparison.OrdinalIgnoreCase))
                    return true;

                int styleType = GetWindowLong(child, GwlStyle) & SsTypeMask;
                if (styleType == SsIcon)
                {
                    IntPtr hIcon = SendMessage(child, StmGetImage, new IntPtr(ImageIcon), IntPtr.Zero);

                    if (hIcon == _hWarning)
                        type = DialogType.Warning;

                    else if (hIcon == _hError)
                        type = DialogType.Error;

                    else if (hIcon == _hQuestion)
                        type = DialogType.Question;

                    else if (hIcon == _hInformation)
                        type = DialogType.Information;
                }
                else
                {
                    StringBuilder text = new(4096);
                    if (GetWindowText(child, text, 4096) > 0)
                        message.AppendLine(text.ToString());
                }

                return true;
            },
            IntPtr.Zero);

        StringBuilder title = new(512);
        GetWindowText(hWnd, title, 512);

        return (type, title.ToString(), message.ToString());
    }
    #endregion

    #region IsDialogVisible
    /// <summary>
    /// Indicates whether the specified window is a visible dialog box.
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns></returns>
    private bool IsDialogVisible(IntPtr hWnd)
    {
        // 1. WS_VISIBLE flag must be set (entire ancestor chain)
        if (!IsWindowVisible(hWnd))
            return false;

        // 2. Not minimised to the taskbar
        if (IsIconic(hWnd))
            return false;

        // 3. Must be a dialog-class window (#32770 covers MessageBox, TaskDialog, etc.)
        StringBuilder cls = new(64);
        GetClassName(hWnd, cls, 64);
        if (cls.ToString() != DialogClass)
            return false;

        // 4. Must occupy screen space — rules out intentionally-hidden zero-size windows
        if (!GetWindowRect(hWnd, out Rect r))
            return false;

        return r.HasPositiveSize;
    }
    #endregion

    #region IsEligibleType
    /// <summary>
    /// Indicates whether the dialog type matches the filter's criteria.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private bool IsEligibleType(ProcessFilter filter, DialogType type) =>
        type switch
        {
            DialogType.Information => filter.AcceptInfo,
            DialogType.Question => filter.AcceptQuestion,
            DialogType.Warning => filter.AcceptWarning,
            DialogType.Error => filter.AcceptError,
            DialogType.Other => filter.AcceptOther,
            _ => false
        };
    #endregion

    #endregion
}