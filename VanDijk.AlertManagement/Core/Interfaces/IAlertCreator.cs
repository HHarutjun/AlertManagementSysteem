/// <summary>
/// Defines a contract for creating alerts from log entries and components.
/// </summary>
public interface IAlertCreator
{
    /// <summary>
    /// Creates an alert based on the provided log and component.
    /// </summary>
    /// <param name="log">The log entry to analyze.</param>
    /// <param name="component">The component associated with the log.</param>
    /// <returns>An Alert object if one is created; otherwise, null.</returns>
    Alert? CreateAlert(string log, string component);
}
