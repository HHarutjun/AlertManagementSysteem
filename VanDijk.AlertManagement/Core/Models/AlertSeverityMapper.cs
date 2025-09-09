/// <summary>
/// Provides methods for mapping string values to <see cref="AlertSeverity"/> enum values.
/// </summary>
public static class AlertSeverityMapper
{
    /// <summary>
    /// Maps a string value to the corresponding <see cref="AlertSeverity"/> enum value.
    /// </summary>
    /// <param name="severityValue">The string representation of the severity.</param>
    /// <returns>The mapped <see cref="AlertSeverity"/> value, or <see cref="AlertSeverity.Unknown"/> if the input is invalid.</returns>
    public static AlertSeverity FromString(string? severityValue)
    {
        if (string.IsNullOrWhiteSpace(severityValue))
        {
            return AlertSeverity.Unknown;
        }

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
