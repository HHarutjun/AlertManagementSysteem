using System;
using System.Collections.Generic;

/// <summary>
/// Strategy for grouping alert logs by their component when severity is Fatal.
/// </summary>
public class CorrelateByComponentStrategy : IAlertGroupingStrategy
{
    /// <summary>
    /// Groups the provided alert logs by their component if the severity is Fatal.
    /// </summary>
    /// <param name="logs">The list of alert logs to group.</param>
    /// <returns>A dictionary mapping component names to lists of fatal alert logs.</returns>
    public IDictionary<string, IList<string>> GroupLogs(IList<string> logs)
    {
        var groupedLogs = new Dictionary<string, IList<string>>();

        foreach (var log in logs)
        {
            var component = log.ExtractComponent();
            var severity = log.ExtractSeverity();

            if (AlertSeverityMapper.FromString(severity) != AlertSeverity.Fatal)
            {
                continue;
            }

            if (!groupedLogs.ContainsKey(component))
            {
                groupedLogs[component] = new List<string>();
            }

            groupedLogs[component].Add(log);
        }

        return groupedLogs;
    }
}
