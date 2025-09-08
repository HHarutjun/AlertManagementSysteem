#nullable enable

/// <summary>
/// Provides functionality to create Alert objects from log entries and component information.
/// </summary>
using System;
using System.Linq;

/// <summary>
/// Implements the IAlertCreator interface to create Alert objects from log entries and component information.
/// </summary>
public class AlertCreator : IAlertCreator
{
    /// <summary>
    /// Creates an <see cref="Alert"/> object from the provided log entries and component information.
    /// </summary>
    /// <param name="log">The log entries as a single string, separated by newlines.</param>
    /// <param name="component">The component or group key associated with the alert.</param>
    /// <returns>An <see cref="Alert"/> object if logs are present; otherwise, <c>null</c>.</returns>
        public Alert? CreateAlert(string log, string component)
        {
            var logs = log.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (logs.Length == 0)
            {
                Console.WriteLine($"[Debug] No logs found for component/groupKey {component}.");
                return null;
            }

            // Zoek eerste log met Fatal, anders pak de eerste log
            var fatalLog = logs.FirstOrDefault(entry =>
                string.Equals(entry.ExtractSeverity(), AlertSeverity.Fatal.ToString(), StringComparison.OrdinalIgnoreCase));
            var firstLog = fatalLog ?? logs.First();

            var severity = fatalLog != null ? AlertSeverity.Fatal.ToString() : firstLog.ExtractSeverity();

            return new Alert
            {
                Message = log,
                Timestamp = firstLog.ExtractTimestamp(),
                Severity = severity,
                Component = component, // Dit is nu de ExceptionType als je ExceptionType strategy gebruikt
                Status = AlertStatus.New,
            };
        }
}
