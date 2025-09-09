using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Sends alerts using multiple alert senders.
/// </summary>
public class MultiAlertSender : IAlertSender
{
    private readonly IEnumerable<IAlertSender> senders;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiAlertSender"/> class.
    /// </summary>
    /// <param name="senders">The collection of alert senders to use.</param>
    public MultiAlertSender(IEnumerable<IAlertSender> senders)
    {
        this.senders = senders;
    }

    /// <summary>
    /// Sends an alert message to all configured alert senders.
    /// </summary>
    /// <param name="message">The alert message to send.</param>
    /// <param name="recipientEmail">The recipient's email address.</param>
    /// <param name="component">The component associated with the alert.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendAlertAsync(string message, string recipientEmail, string component)
    {
        foreach (var sender in this.senders)
        {
            try
            {
                await sender.SendAlertAsync(message, recipientEmail, component);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Fout bij alert sender: {ex.Message}");
            }
        }
    }
}
