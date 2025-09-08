using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Processes logs using the provided ILogProvider implementation.
/// </summary>
public class LogProcessor : ILogProcessor
{
    private readonly ILogProvider logProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogProcessor"/> class.
    /// </summary>
    /// <param name="logProvider">The log provider to use for fetching logs.</param>
    public LogProcessor(ILogProvider logProvider)
    {
        this.logProvider = logProvider;
    }

    /// <summary>
    /// Asynchronously fetches logs using the configured log provider.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of log entries.</returns>
    public async Task<List<string>> FetchLogsAsync()
    {
        return await this.logProvider.FetchLogsAsync();
    }
}
