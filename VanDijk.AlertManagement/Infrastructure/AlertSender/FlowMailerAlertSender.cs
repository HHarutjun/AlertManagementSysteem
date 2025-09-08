using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class FlowMailerAlertSender : IAlertSender
{
    private readonly string _apiEndpoint;
    private readonly string _oauthEndpoint;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _accountId;

    private static string GetTemplatePath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var templatePath = Path.Combine(baseDir, "..", "Presentation", "Templates", "AlertTemplate.html");
        return Path.GetFullPath(templatePath);
    }

    public FlowMailerAlertSender(string apiEndpoint, string oauthEndpoint, string clientId, string clientSecret, string accountId)
    {
        _apiEndpoint = apiEndpoint;
        _oauthEndpoint = oauthEndpoint;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _accountId = accountId;
    }

    public async Task SendAlertAsync(string message, string recipientEmail, string component)
    {
        Console.WriteLine($"[FlowMailerAlertSender] Sending email to {recipientEmail} for component {component}");
        var accessToken = await GetAccessTokenAsync();

        string htmlBody = AlertTemplateRenderer.RenderHtmlBody(message, component);

        var emailPayload = new
        {
            messageType = "EMAIL",
            senderAddress = "info@vandijk.nl",
            recipientAddress = recipientEmail,
            subject = $"Alert for {component}",
            html = htmlBody,
            text = $"{message}\nComponent: {component}"
        };

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var payloadJson = JsonSerializer.Serialize(emailPayload);
        var endpoint = $"{_apiEndpoint}/{_accountId}/messages/submit";

        try
        {
            var response = await httpClient.PostAsync(
                endpoint,
                new StringContent(payloadJson, Encoding.UTF8, "application/json")
            );

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

    private static string ExtractField(string message, string field)
    {
        var idx = message.IndexOf(field, StringComparison.OrdinalIgnoreCase);
        if (idx == -1) return "";
        idx += field.Length;
        var end = message.IndexOfAny(new[] { '|', '\n' }, idx);
        if (end == -1) end = message.Length;
        return message.Substring(idx, end - idx).Trim();
    }

    private async Task<string> GetAccessTokenAsync()
    {
        using var httpClient = new HttpClient();

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret)
        });

        var response = await httpClient.PostAsync(_oauthEndpoint, tokenRequest);
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
}
