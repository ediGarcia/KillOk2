using KillOk2.Models;

namespace KillOk2.Services;

public interface ISystemService
{
    /// <summary>
    /// Closes all visible dialog windows owned by the specified process, and returns their info.
    /// </summary>
    /// <param name="filters"></param>
    /// <returns></returns>
    IEnumerable<DialogInfo> CloseDialogs(IEnumerable<ProcessFilter> filters);

    /// <summary>
    /// Loads persisted filters from the file system.
    /// </summary>
    /// <returns></returns>
    IEnumerable<ProcessFilter>? LoadPersistedFilters();

    /// <summary>
    /// Persists filters into the file system.
    /// </summary>
    /// <param name="filters"></param>
    void PersistFilters(IEnumerable<ProcessFilter> filters);
}