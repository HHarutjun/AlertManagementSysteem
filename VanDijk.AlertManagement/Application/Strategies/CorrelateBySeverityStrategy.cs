using System;
using System.Collections.Generic;

/// <summary>
/// Strategy for grouping logs by their severity level.
/// </summary>
public class CorrelateBySeverityStrategy : IAlertGroupingStrategy
{
    /// <summary>
    /// Groups the provided logs by their severity.
    /// </summary>
    /// <param name="logs">A list of log entries to group.</param>
    /// <returns>A dictionary where the key is the severity and the value is a list of logs with that severity.</returns>
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
