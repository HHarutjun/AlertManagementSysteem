using System;
using System.Collections.Generic;

public class CorrelateByExceptionTypeStrategy : IAlertGroupingStrategy
{
    public IDictionary<string, IList<string>> GroupLogs(IList<string> logs)
    {
        var groupedLogs = new Dictionary<string, IList<string>>();

        foreach (var log in logs)
        {
            var exceptionType = log.ExtractExceptionType();
            if (string.IsNullOrWhiteSpace(exceptionType))
                continue;

            var key = exceptionType.Trim();

            if (!groupedLogs.ContainsKey(key))
                groupedLogs[key] = new List<string>();

            // Voeg alleen unieke logs toe
            if (!groupedLogs[key].Contains(log))
                groupedLogs[key].Add(log);
        }

        return groupedLogs;
    }
}
