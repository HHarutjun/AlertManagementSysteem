using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class TeamsUserAlertSender : IAlertSender
{
    private readonly string _tenantId;
    private readonly string _clientId;
    private readonly string _clientSecret;

    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";
    private const string TokenUrlTemplate = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";

    public TeamsUserAlertSender(string tenantId, string clientId, string clientSecret)
    {
        _tenantId = tenantId;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    private static string GetTemplatePath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var templatePath = Path.Combine(baseDir, "..", "Presentation", "Templates", "AlertTemplate.html");
        return Path.GetFullPath(templatePath);
    }

    public async Task SendAlertAsync(string message, string recipientEmail, string component)
    {
        var token = await GetAccessTokenAsync();
        var userId = await GetUserIdByEmailAsync(recipientEmail, token);
        var chatId = await GetOrCreateChatWithUserAsync(userId, token);

        string htmlBody = AlertTemplateRenderer.RenderHtmlBody(message, component);

        await SendTeamsMessageAsync(chatId, htmlBody, token);
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

    private class TokenResponse
    {
        public string access_token { get; set; }
    }

    private async Task<string> GetAccessTokenAsync()
    {
        using var http = new HttpClient();

        var url = string.Format(TokenUrlTemplate, _tenantId);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("scope", $"{GraphBaseUrl}/.default"),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var resp = await http.PostAsync(url, content);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Failed to get token: {json}");

        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);
        return tokenResponse?.access_token;
    }

    private async Task<string> GetUserIdByEmailAsync(string email, string token)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await http.GetAsync($"{GraphBaseUrl}/users/{email}");
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Failed to get user: {json}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString();
    }

    private async Task<string> GetOrCreateChatWithUserAsync(string userId, string token)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var chatPayload = new Dictionary<string, object>
        {
            { "chatType", "oneOnOne" },
            { "members", new[]
                {
                    new Dictionary<string, object>
                    {
                        { "@odata.type", "#microsoft.graph.aadUserConversationMember" },
                        { "roles", new[] { "owner" } },
                        { "user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{userId}')" }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(chatPayload);

        var resp = await http.PostAsync($"{GraphBaseUrl}/chats", new StringContent(json, Encoding.UTF8, "application/json"));
        var respJson = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Failed to create chat: {respJson}");

        using var doc = JsonDocument.Parse(respJson);
        return doc.RootElement.GetProperty("id").GetString();
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
            throw new Exception($"Failed to send Teams message: {respJson}");
    }
}
