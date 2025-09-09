using System;

/// <summary>
/// Represents an alert with message, severity, timestamp, component, and status.
/// </summary>
public interface IAlert
{
    /// <summary>
    /// Gets or sets the alert message.
    /// </summary>
    string Message { get; set; }

    /// <summary>
    /// Gets or sets the severity of the alert.
    /// </summary>
    string Severity { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the alert.
    /// </summary>
    DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the component associated with the alert.
    /// </summary>
    string Component { get; set; }

    /// <summary>
    /// Gets or sets the status of the alert.
    /// </summary>
    string Status { get; set; }

    /// <summary>
    /// Marks the alert as in progress.
    /// </summary>
    void MarkInProgress();

    /// <summary>
    /// Marks the alert as resolved.
    /// </summary>
    void MarkResolved();
}
