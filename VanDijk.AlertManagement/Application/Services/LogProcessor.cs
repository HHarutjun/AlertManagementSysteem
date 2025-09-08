using System.Collections.Generic;
using System.Threading.Tasks;
public class LogProcessor : ILogProcessor
{
    private readonly ILogProvider _logProvider;

    public LogProcessor(ILogProvider logProvider)
    {
        _logProvider = logProvider;
    }

    public async Task<List<string>> FetchLogsAsync()
    {
        return await _logProvider.FetchLogsAsync();
    }
}
