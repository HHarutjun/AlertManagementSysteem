using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Sends alert messages to Microsoft Teams users via Microsoft Graph API.
/// </summary>
public class TeamsUserAlertSender : IAlertSender
{
    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";
    private const string TokenUrlTemplate = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";

    private readonly string tenantId;
    private readonly string clientId;
    private readonly string clientSecret;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamsUserAlertSender"/> class.
    /// </summary>
    /// <param name="tenantId">The Azure AD tenant ID.</param>
    /// <param name="clientId">The Azure AD application client ID.</param>
    /// <param name="clientSecret">The Azure AD application client secret.</param>
    public TeamsUserAlertSender(string tenantId, string clientId, string clientSecret)
    {
        this.tenantId = tenantId;
        this.clientId = clientId;
        this.clientSecret = clientSecret;
    }

    /// <summary>
    /// Sends an alert message to a specified Microsoft Teams user.
    /// </summary>
    /// <param name="message">The alert message to send.</param>
    /// <param name="recipientEmail">The email address of the recipient user.</param>
    /// <param name="component">The component associated with the alert.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendAlertAsync(string message, string recipientEmail, string component)
    {
        var token = await this.GetAccessTokenAsync();
        var userId = await this.GetUserIdByEmailAsync(recipientEmail, token);
        var chatId = await this.GetOrCreateChatWithUserAsync(userId, token);

        string htmlBody = AlertTemplateRenderer.RenderHtmlBody(message, component);

        await this.SendTeamsMessageAsync(chatId, htmlBody, token);
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

    private static string GetTemplatePath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var templatePath = Path.Combine(baseDir, "..", "Presentation", "Templates", "AlertTemplate.html");
        return Path.GetFullPath(templatePath);
    }

    private async Task<string> GetAccessTokenAsync()
    {
        using var http = new HttpClient();

        var url = string.Format(TokenUrlTemplate, this.tenantId);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", this.clientId),
            new KeyValuePair<string, string>("scope", $"{GraphBaseUrl}/.default"),
            new KeyValuePair<string, string>("client_secret", this.clientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
        });

        var resp = await http.PostAsync(url, content);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get token: {json}");
        }

        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);
        if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
        {
            throw new Exception("Access token is null or empty.");
        }

        return tokenResponse.AccessToken;
    }

    private async Task<string> GetUserIdByEmailAsync(string email, string token)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await http.GetAsync($"{GraphBaseUrl}/users/{email}");
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get user: {json}");
        }

        using var doc = JsonDocument.Parse(json);
        var userId = doc.RootElement.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(userId))
        {
            throw new Exception("User ID is null or empty.");
        }

        return userId;
    }

    private async Task<string> GetOrCreateChatWithUserAsync(string userId, string token)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var chatPayload = new Dictionary<string, object>
        {
            { "chatType", "oneOnOne" },
            {
                "members", new[]
                {
                    new Dictionary<string, object>
                    {
                        { "@odata.type", "#microsoft.graph.aadUserConversationMember" },
                        { "roles", new[] { "owner" } },
                        { "user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{userId}')" },
                    },
                }
            },
        };

        var json = JsonSerializer.Serialize(chatPayload);

        var resp = await http.PostAsync($"{GraphBaseUrl}/chats", new StringContent(json, Encoding.UTF8, "application/json"));
        var respJson = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create chat: {respJson}");
        }

        using var doc = JsonDocument.Parse(respJson);
        var chatId = doc.RootElement.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(chatId))
        {
            throw new Exception("Chat ID is null or empty.");
        }

        return chatId;
    }

    private async Task SendTeamsMessageAsync(string chatId, string message, string token)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new { body = new { content = message } };
        var json = JsonSerializer.Serialize(payload);

        var resp = await http.PostAsync($"{GraphBaseUrl}/chats/{chatId}/messages", new StringContent(json, Encoding.UTF8, "application/json"));
        var respJson = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to send Teams message: {respJson}");
        }
    }

    private class TokenResponse
    {
        public string? AccessToken { get; set; }
    }
}
