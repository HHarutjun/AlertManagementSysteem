using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

/// <summary>
/// Sends alert emails using SMTP with a specified template and credentials.
/// </summary>
public class SmtpAlertSender : IAlertSender
{
    private readonly string smtpHost;
    private readonly int smtpPort;
    private readonly string smtpUser;
    private readonly string smtpPass;
    private readonly string fromEmail;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmtpAlertSender"/> class with SMTP configuration and sender email.
    /// </summary>
    /// <param name="smtpHost">The SMTP server host.</param>
    /// <param name="smtpPort">The SMTP server port.</param>
    /// <param name="smtpUser">The SMTP username.</param>
    /// <param name="smtpPass">The SMTP password.</param>
    /// <param name="fromEmail">The sender's email address.</param>
    public SmtpAlertSender(string smtpHost, int smtpPort, string smtpUser, string smtpPass, string fromEmail)
    {
        this.smtpHost = smtpHost;
        this.smtpPort = smtpPort;
        this.smtpUser = smtpUser;
        this.smtpPass = smtpPass;
        this.fromEmail = fromEmail;
    }

    /// <summary>
    /// Sends an alert email asynchronously to the specified recipient with the given message and component name.
    /// </summary>
    /// <param name="message">The alert message to include in the email body.</param>
    /// <param name="recipientEmail">The recipient's email address.</param>
    /// <param name="component">The name of the component sending the alert.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    public async Task SendAlertAsync(string message, string recipientEmail, string component)
    {
        Console.WriteLine($"[SMTP] Preparing to send mail via {this.smtpHost}:{this.smtpPort} as {this.smtpUser} (from: {this.fromEmail}) to {recipientEmail}");

        string htmlBody = AlertTemplateRenderer.RenderHtmlBody(message, component);

        using (var client = new SmtpClient(this.smtpHost, this.smtpPort))
        {
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(this.smtpUser, this.smtpPass);

            var mail = new MailMessage(this.fromEmail, recipientEmail)
            {
                Subject = $"Alert van component: {component}",
                Body = htmlBody,
                IsBodyHtml = true,
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
                {
                    Console.WriteLine($"[SMTP][Error] InnerException: {smtpEx.InnerException.Message}");
                }

                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SMTP][Error] General Exception: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[SMTP][Error] InnerException: {ex.InnerException.Message}");
                }

                throw;
            }
        }
    }

    private static string GetTemplatePath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var templatePath = Path.Combine(baseDir, "..", "Presentation", "Templates", "AlertTemplate.html");
        return Path.GetFullPath(templatePath);
    }

    private static string ExtractField(string message, string field)
    {
        var idx = message.IndexOf(field, StringComparison.OrdinalIgnoreCase);
        if (idx == -1)
        {
            return string.Empty;
        }

        idx += field.Length;
        var end = message.IndexOfAny(new[] { '|', '\n' }, idx);
        if (end == -1)
        {
            end = message.Length;
        }

        return message.Substring(idx, end - idx).Trim();
    }
}
