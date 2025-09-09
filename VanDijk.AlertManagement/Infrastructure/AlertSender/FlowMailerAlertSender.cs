using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Sends alert emails using the FlowMailer service.
/// </summary>
public class FlowMailerAlertSender : IAlertSender
{
    private readonly string apiEndpoint;
    private readonly string oauthEndpoint;
    private readonly string clientId;
    private readonly string clientSecret;
    private readonly string accountId;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlowMailerAlertSender"/> class.
    /// </summary>
    /// <param name="apiEndpoint">The API endpoint for FlowMailer.</param>
    /// <param name="oauthEndpoint">The OAuth endpoint for authentication.</param>
    /// <param name="clientId">The client ID for OAuth authentication.</param>
    /// <param name="clientSecret">The client secret for OAuth authentication.</param>
    /// <param name="accountId">The account ID for FlowMailer.</param>
    public FlowMailerAlertSender(string apiEndpoint, string oauthEndpoint, string clientId, string clientSecret, string accountId)
    {
        this.apiEndpoint = apiEndpoint;
        this.oauthEndpoint = oauthEndpoint;
        this.clientId = clientId;
        this.clientSecret = clientSecret;
        this.accountId = accountId;
    }

    /// <summary>
    /// Checks the status of a message using the provided message URL and access token.
    /// </summary>
    /// <param name="messageUrl">The URL of the message to check.</param>
    /// <param name="accessToken">The OAuth access token for authentication.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CheckMessageStatusAsync(string messageUrl, string accessToken)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.GetAsync(messageUrl);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to retrieve message status: {response.StatusCode} - {responseContent}");
        }
    }

    /// <summary>
    /// Sends an alert email asynchronously using the FlowMailer service.
    /// </summary>
    /// <param name="message">The alert message to send.</param>
    /// <param name="recipientEmail">The recipient's email address.</param>
    /// <param name="component">The component related to the alert.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendAlertAsync(string message, string recipientEmail, string component)
    {
        Console.WriteLine($"[FlowMailerAlertSender] Sending email to {recipientEmail} for component {component}");
        var accessToken = await this.GetAccessTokenAsync();

        string htmlBody = AlertTemplateRenderer.RenderHtmlBody(message, component);

        var emailPayload = new
        {
            messageType = "EMAIL",
            senderAddress = "info@vandijk.nl",
            recipientAddress = recipientEmail,
            subject = $"Alert for {component}",
            html = htmlBody,
            text = $"{message}\nComponent: {component}",
        };

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var payloadJson = JsonSerializer.Serialize(emailPayload);
        var endpoint = $"{this.apiEndpoint}/{this.accountId}/messages/submit";

        try
        {
            var response = await httpClient.PostAsync(
                endpoint,
                new StringContent(payloadJson, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Error] Failed to send email: {response.StatusCode} - {responseContent}");
                throw new Exception($"Failed to send email: {response.StatusCode} - {responseContent}");
            }

            Console.WriteLine($"[FlowMailerAlertSender] Email successfully sent to {recipientEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Exception while sending email to {recipientEmail}: {ex.Message}");
            throw;
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

    private async Task<string> GetAccessTokenAsync()
    {
        using var httpClient = new HttpClient();

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", this.clientId),
            new KeyValuePair<string, string>("client_secret", this.clientSecret),
        });

        var response = await httpClient.PostAsync(this.oauthEndpoint, tokenRequest);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            if (tokenResponse.TryGetProperty("access_token", out var accessToken))
            {
                return accessToken.GetString() ?? throw new Exception("Access token is null.");
            }

            throw new Exception("Access token not found in response.");
        }
        else
        {
            throw new Exception($"Failed to retrieve access token: {response.StatusCode} - {responseContent}");
        }
    }
}