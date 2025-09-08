using System.Collections.Generic;

public interface IAlertGroupingStrategy
{
    IDictionary<string, IList<string>> GroupLogs(IList<string> logs);
}
