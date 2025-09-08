using System;

public class Alert : IAlert
{
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Component { get; set; } = string.Empty;

    public AlertStatus Status { get; set; } = AlertStatus.New;

    string IAlert.Status
    {
        get => Status.ToString();
        set
        {
            if (Enum.TryParse<AlertStatus>(value, out var status))
            {
                Status = status;
            }
            else
            {
                throw new ArgumentException($"Invalid status value: {value}");
            }
        }
    }

    public void MarkInProgress() => Status = AlertStatus.InProgress;
    public void MarkResolved() => Status = AlertStatus.Resolved;
}
