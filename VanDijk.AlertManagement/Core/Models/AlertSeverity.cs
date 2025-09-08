public enum AlertSeverity
{
    Fatal,
    Warning,
    Info,
    Unknown
}

public static class AlertSeverityMapper
{
    public static AlertSeverity FromString(string? severityValue)
    {
        if (string.IsNullOrWhiteSpace(severityValue))
            return AlertSeverity.Unknown;
        var val = severityValue.Trim();
        return val.ToLowerInvariant() switch
        {
            "3" => AlertSeverity.Fatal,
            "2" => AlertSeverity.Warning,
            "1" => AlertSeverity.Info,
            "fatal" => AlertSeverity.Fatal,
            "warning" => AlertSeverity.Warning,
            "info" => AlertSeverity.Info,
            _ => AlertSeverity.Unknown
        };
    }
}
