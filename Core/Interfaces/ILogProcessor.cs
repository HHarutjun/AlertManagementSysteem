using System.Collections.Generic;
using System.Threading.Tasks;

public interface ILogProcessor
{
    Task<List<string>> FetchLogsAsync();
}
