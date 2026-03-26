using System.Diagnostics;

namespace KillOk2.Services;

public static class LoggerService
{
    #region Public Methods

    #region WriteDebugLine
    /// <inheritdoc cref="Debug.WriteLine(string)"/>
    [Conditional("DEBUG")]
    public static void WriteDebugLine(string message) =>
Debug.WriteLine($"{DateTime.Now:HH:mm:ss} - {message}");
    #endregion

    #endregion
}