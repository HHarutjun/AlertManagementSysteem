using System.Threading.Tasks;

public interface IAlertSender
{
    Task SendAlertAsync(string message, string recipientEmail, string component);
}
