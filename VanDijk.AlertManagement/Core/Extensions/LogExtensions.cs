using System;
using System.Globalization;

/// <summary>
/// Provides extension methods for extracting information from log strings.
/// </summary>
public static class LogExtensions
{
    /// <summary>
    /// Extracts the component name from the log string.
    /// </summary>
    /// <param name="log">The log string to extract the component from.</param>
    /// <returns>The extracted component name, or "Unknown" if not found.</returns>
    public static string ExtractComponent(this string log)
    {
        var component = ExtractValueByPrefix(log, "Component: ");
        if (!string.IsNullOrEmpty(component))
        {
            return component;
        }

        var endpoint = ExtractValueByPrefix(log, "Endpoint: ");
        if (!string.IsNullOrEmpty(endpoint))
        {
            return endpoint;
        }

        var parts = log.Split('|');
        if (parts.Length >= 3)
        {
            return parts[2].Trim();
        }

        return "Unknown";
    }

    /// <summary>
    /// Extracts the severity from the log string.
    /// </summary>
    /// <param name="log">The log string to extract the severity from.</param>
    /// <returns>The extracted severity as a string.</returns>
    public static string ExtractSeverity(this string log)
    {
        var severityValue = ExtractValueByPrefix(log, "Severity: ");
        if (!string.IsNullOrEmpty(severityValue))
        {
            return AlertSeverityMapper.FromString(severityValue).ToString();
        }

        var statusValue = ExtractValueByPrefix(log, "Status: ");
        if (!string.IsNullOrEmpty(statusValue))
        {
            if (statusValue.Equals("Down", StringComparison.OrdinalIgnoreCase))
            {
                return AlertSeverity.Fatal.ToString();
            }

            if (statusValue.Equals("Up", StringComparison.OrdinalIgnoreCase))
            {
                return AlertSeverity.Info.ToString();
            }

            if (statusValue.Equals("Warning", StringComparison.OrdinalIgnoreCase))
            {
                return AlertSeverity.Warning.ToString();
            }

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

    /// <summary>
    /// Extracts the problem ID from the log string.
    /// </summary>
    /// <param name="log">The log string to extract the problem ID from.</param>
    /// <returns>The extracted problem ID, or the original log string if not found.</returns>
    public static string ExtractProblemId(this string log)
    {
        var problemId = ExtractValueByPrefix(log, "ProblemId: ");
        if (!string.IsNullOrEmpty(problemId))
        {
            return problemId;
        }

        var parts = log.Split('|');
        if (parts.Length >= 4)
        {
            return parts[3].Trim();
        }

        return log;
    }

    /// <summary>
    /// Extracts the timestamp from the log string.
    /// </summary>
    /// <param name="log">The log string to extract the timestamp from.</param>
    /// <returns>The extracted timestamp as a <see cref="DateTime"/>. Returns <see cref="DateTime.MinValue"/> if not found or invalid.</returns>
    public static DateTime ExtractTimestamp(this string log)
    {
        var timestampString = ExtractValueByPrefix(log, "Timestamp: ");
        if (!string.IsNullOrEmpty(timestampString))
        {
            if (DateTime.TryParse(timestampString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            {
                return dt;
            }
        }

        var parts = log.Split('|');
        if (parts.Length >= 1)
        {
            var ts = parts[0].Trim();
            if (ts.StartsWith("Timestamp: "))
            {
                ts = ts.Substring("Timestamp: ".Length);
            }

            if (DateTime.TryParse(ts, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            {
                return dt;
            }
        }

        return DateTime.MinValue;
    }

    /// <summary>
    /// Extracts the exception type from the log string.
    /// </summary>
    /// <param name="log">The log string to extract the exception type from.</param>
    /// <returns>The extracted exception type, or null if not found.</returns>
    public static string? ExtractExceptionType(this string log)
    {
        return ExtractValueByPrefix(log, "ExceptionType: ");
    }

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
}
