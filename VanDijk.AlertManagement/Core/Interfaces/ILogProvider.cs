using System.Collections.Generic;
using System.Threading.Tasks;

public interface ILogProvider
{
    Task<List<string>> FetchLogsAsync();
}
