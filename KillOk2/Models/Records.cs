namespace KillOk2.Models;

public record ProcessFilter(
    string ProcessName,
    bool AcceptInfo,
    bool AcceptQuestion,
    bool AcceptWarning,
    bool AcceptError,
    bool AcceptOther);

public record DialogInfo(
    DialogType Type,
    string Title,
    string Message,
    string ProcessName,
    int ProcessId,
    DateTime ClosedTime);