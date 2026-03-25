using System.Windows.Media.Imaging;

namespace KillOk2.ViewModels;

public class DialogInfoViewModel(DialogType type, string title, string message, int processId, string? processName, BitmapSource processIcon, DateTime timestamp)
{
    #region Properties

    /// <summary>
    /// Gets the closed dialog message.
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// Gets the closed dialog owner process id.
    /// </summary>
    public int ProcessId { get; } = processId;

    /// <summary>
    /// Gets the closed dialog owner process name.
    /// </summary>
    public string? ProcessName { get; } = processName;

    /// <summary>
    /// Gets the closed dialog owner process icon.
    /// </summary>
    public BitmapSource ProcessIcon { get; } = processIcon;

    /// <summary>
    /// Gets the moment in which the dialog was closed.
    /// </summary>
    public DateTime Timestamp { get; } = timestamp;

    /// <summary>
    /// Gets the closed dialog title.
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    /// Gets the closed dialog type.
    /// </summary>
    public DialogType Type { get; } = type;

    #endregion
}