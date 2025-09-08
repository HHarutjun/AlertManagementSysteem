using System.Collections.Generic;
using System.Threading.Tasks;

public class MultiAlertSender : IAlertSender
{
    private readonly IEnumerable<IAlertSender> _senders;

    public MultiAlertSender(IEnumerable<IAlertSender> senders)
    {
        _senders = senders;
    }

    public async Task SendAlertAsync(string message, string recipientEmail, string component)
    {
        foreach (var sender in _senders)
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
