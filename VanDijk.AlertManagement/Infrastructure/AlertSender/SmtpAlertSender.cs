using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.IO;

public class SmtpAlertSender : IAlertSender
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;
    private readonly string _fromEmail;

    private static string GetTemplatePath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var templatePath = Path.Combine(baseDir, "..", "Presentation", "Templates", "AlertTemplate.html");
        return Path.GetFullPath(templatePath);
    }

    public SmtpAlertSender(string smtpHost, int smtpPort, string smtpUser, string smtpPass, string fromEmail)
    {
        _smtpHost = smtpHost;
        _smtpPort = smtpPort;
        _smtpUser = smtpUser;
        _smtpPass = smtpPass;
        _fromEmail = fromEmail;
    }

    public async Task SendAlertAsync(string message, string recipientEmail, string component)
    {
        Console.WriteLine($"[SMTP] Preparing to send mail via {_smtpHost}:{_smtpPort} as {_smtpUser} (from: {_fromEmail}) to {recipientEmail}");

        string htmlBody = AlertTemplateRenderer.RenderHtmlBody(message, component);

        using (var client = new SmtpClient(_smtpHost, _smtpPort))
        {
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);

            var mail = new MailMessage(_fromEmail, recipientEmail)
            {
                Subject = $"Alert van component: {component}",
                Body = htmlBody,
                IsBodyHtml = true
            };

            try
            {
                Console.WriteLine("[SMTP] Attempting to send email...");
                await client.SendMailAsync(mail);
                Console.WriteLine("[SMTP] Email sent successfully.");
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"[SMTP][Error] SMTP Exception: {smtpEx.StatusCode} - {smtpEx.Message}");
                if (smtpEx.InnerException != null)
                    Console.WriteLine($"[SMTP][Error] InnerException: {smtpEx.InnerException.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SMTP][Error] General Exception: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[SMTP][Error] InnerException: {ex.InnerException.Message}");
                throw;
            }
        }
    }

    private static string ExtractField(string message, string field)
    {
        var idx = message.IndexOf(field, StringComparison.OrdinalIgnoreCase);
        if (idx == -1) return "";
        idx += field.Length;
        var end = message.IndexOfAny(new[] { '|', '\n' }, idx);
        if (end == -1) end = message.Length;
        return message.Substring(idx, end - idx).Trim();
    }
}
