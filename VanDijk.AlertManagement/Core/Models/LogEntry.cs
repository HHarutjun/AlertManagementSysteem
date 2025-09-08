using System;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
