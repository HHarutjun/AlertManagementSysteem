using System;
using System.Collections.Generic;

public class CorrelateBySeverityStrategy : IAlertGroupingStrategy
{
    public IDictionary<string, IList<string>> GroupLogs(IList<string> logs)
    {
        var groupedLogs = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var log in logs)
        {
            var severity = log.ExtractSeverity();
            if (!groupedLogs.ContainsKey(severity))
            {
                groupedLogs[severity] = new List<string>();
            }
            groupedLogs[severity].Add(log);
        }

        return groupedLogs;
    }
}
