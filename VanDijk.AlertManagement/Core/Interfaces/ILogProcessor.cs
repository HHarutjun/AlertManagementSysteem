using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Defines a contract for processing and fetching logs.
/// </summary>
public interface ILogProcessor
{
    /// <summary>
    /// Asynchronously fetches a list of logs.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of log strings.</returns>
    Task<List<string>> FetchLogsAsync();
}
