using System;

/// <summary>
/// Represents an alert with a message, severity, timestamp, component, and status.
/// </summary>
public class Alert : IAlert
{
    /// <summary>
    /// Gets or sets the alert message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity of the alert.
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the alert.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the component associated with the alert.
    /// </summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of the alert.
    /// </summary>
    public AlertStatus Status { get; set; } = AlertStatus.New;

    /// <summary>
    /// Gets or sets the status of the alert as a string for the IAlert interface.
    /// </summary>
    string IAlert.Status
    {
        get => this.Status.ToString();
        set
        {
            if (Enum.TryParse<AlertStatus>(value, out var status))
            {
                this.Status = status;
            }
            else
            {
                throw new ArgumentException($"Invalid status value: {value}");
            }
        }
    }

    /// <summary>
    /// Marks the alert as in progress by setting its status to InProgress.
    /// </summary>
    public void MarkInProgress() => this.Status = AlertStatus.InProgress;

    /// <summary>
    /// Marks the alert as resolved by setting its status to Resolved.
    /// </summary>
    public void MarkResolved() => this.Status = AlertStatus.Resolved;
}
