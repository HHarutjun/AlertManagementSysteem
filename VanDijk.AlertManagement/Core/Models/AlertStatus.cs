/// <summary>
/// Represents the status of an alert.
/// </summary>
public enum AlertStatus
{
    /// <summary>
    /// The alert is newly created and has not been processed yet.
    /// </summary>
    New,

    /// <summary>
    /// The alert is currently being processed.
    /// </summary>
    InProgress,

    /// <summary>
    /// The alert has been resolved.
    /// </summary>
    Resolved,
}
