using System;
using System.Collections.Generic;

public class CorrelateByComponentStrategy : IAlertGroupingStrategy
{
    public IDictionary<string, IList<string>> GroupLogs(IList<string> logs)
    {
        var groupedLogs = new Dictionary<string, IList<string>>();

        foreach (var log in logs)
        {
            var component = log.ExtractComponent();
            var severity = log.ExtractSeverity();

            if (AlertSeverityMapper.FromString(severity) != AlertSeverity.Fatal)
                continue;

            if (!groupedLogs.ContainsKey(component))
            {
                groupedLogs[component] = new List<string>();
            }
            groupedLogs[component].Add(log);
        }

        return groupedLogs;
    }
}
