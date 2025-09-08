using System;
using System.Linq;

public class AlertCreator : IAlertCreator
{
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
            Status = AlertStatus.New
        };
    }
}
