using System.Threading.Tasks;

/// <summary>
/// Defines a contract for sending alerts.
/// </summary>
public interface IAlertSender
{
    /// <summary>
    /// Sends an alert asynchronously.
    /// </summary>
    /// <param name="message">The alert message to send.</param>
    /// <param name="recipientEmail">The recipient's email address.</param>
    /// <param name="component">The component related to the alert.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendAlertAsync(string message, string recipientEmail, string component);
}
