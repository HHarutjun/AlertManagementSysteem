using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Provides an interface for fetching logs asynchronously.
/// </summary>
public interface ILogProvider
{
    /// <summary>
    /// Fetches logs asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of log strings.</returns>
    Task<List<string>> FetchLogsAsync();
}
