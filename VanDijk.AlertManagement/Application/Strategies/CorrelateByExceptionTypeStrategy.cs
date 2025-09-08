using System;
using System.Collections.Generic;

/// <summary>
/// Strategy for grouping logs by their exception type.
/// </summary>
public class CorrelateByExceptionTypeStrategy : IAlertGroupingStrategy
{
    /// <summary>
    /// Groups the provided logs by their extracted exception type.
    /// </summary>
    /// <param name="logs">A list of log entries to group.</param>
    /// <returns>A dictionary where the key is the exception type and the value is a list of logs with that exception type.</returns>
    public IDictionary<string, IList<string>> GroupLogs(IList<string> logs)
    {
        var groupedLogs = new Dictionary<string, IList<string>>();

        foreach (var log in logs)
        {
            var exceptionType = log.ExtractExceptionType();
            if (string.IsNullOrWhiteSpace(exceptionType))
            {
                continue;
            }

            var key = exceptionType.Trim();

            if (!groupedLogs.ContainsKey(key))
            {
                groupedLogs[key] = new List<string>();
            }

            // Voeg alleen unieke logs toe
            if (!groupedLogs[key].Contains(log))
            {
                groupedLogs[key].Add(log);
            }
        }

        return groupedLogs;
    }
}
