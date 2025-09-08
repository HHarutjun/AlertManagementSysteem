using System;
using System.Globalization;

public static class LogExtensions
{
    private static string? ExtractValueByPrefix(string log, string prefix)
    {
        var startIndex = log.IndexOf(prefix, StringComparison.Ordinal);
        if (startIndex != -1)
        {
            startIndex += prefix.Length;
            var endIndex = log.IndexOf(" |", startIndex, StringComparison.Ordinal);
            return endIndex > startIndex
                ? log.Substring(startIndex, endIndex - startIndex).Trim()
                : log.Substring(startIndex).Trim();
        }
        return null;
    }

    public static string ExtractComponent(this string log)
    {
        var component = ExtractValueByPrefix(log, "Component: ");
        if (!string.IsNullOrEmpty(component))
            return component;

        var endpoint = ExtractValueByPrefix(log, "Endpoint: ");
        if (!string.IsNullOrEmpty(endpoint))
            return endpoint;

        var parts = log.Split('|');
        if (parts.Length >= 3)
            return parts[2].Trim();
        return "Unknown";
    }

    public static string ExtractSeverity(this string log)
    {
        var severityValue = ExtractValueByPrefix(log, "Severity: ");
        if (!string.IsNullOrEmpty(severityValue))
            return AlertSeverityMapper.FromString(severityValue).ToString();

        var statusValue = ExtractValueByPrefix(log, "Status: ");
        if (!string.IsNullOrEmpty(statusValue))
        {
            if (statusValue.Equals("Down", StringComparison.OrdinalIgnoreCase))
                return AlertSeverity.Fatal.ToString();
            if (statusValue.Equals("Up", StringComparison.OrdinalIgnoreCase))
                return AlertSeverity.Info.ToString();
            if (statusValue.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                return AlertSeverity.Warning.ToString();
            return AlertSeverityMapper.FromString(statusValue).ToString();
        }
        var parts = log.Split('|');
        if (parts.Length >= 2)
        {
            var pipeSeverity = parts[1].Trim();
            return AlertSeverityMapper.FromString(pipeSeverity).ToString();
        }
        return AlertSeverity.Unknown.ToString();
    }

    public static string ExtractProblemId(this string log)
    {
        var problemId = ExtractValueByPrefix(log, "ProblemId: ");
        if (!string.IsNullOrEmpty(problemId))
            return problemId;

        var parts = log.Split('|');
        if (parts.Length >= 4)
            return parts[3].Trim();
        return log;
    }

    public static DateTime ExtractTimestamp(this string log)
    {
        var timestampString = ExtractValueByPrefix(log, "Timestamp: ");
        if (!string.IsNullOrEmpty(timestampString))
        {
            if (DateTime.TryParse(timestampString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
                return dt;
        }
        var parts = log.Split('|');
        if (parts.Length >= 1)
        {
            var ts = parts[0].Trim();
            if (ts.StartsWith("Timestamp: "))
                ts = ts.Substring("Timestamp: ".Length);
            if (DateTime.TryParse(ts, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
                return dt;
        }
        return DateTime.MinValue;
    }

    public static string? ExtractExceptionType(this string log)
    {
        return ExtractValueByPrefix(log, "ExceptionType: ");
    }
}
