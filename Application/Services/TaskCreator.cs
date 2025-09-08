using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class TaskCreator : ITaskCreator
{
    private readonly HashSet<string> _existingTasks;
    private readonly string _organizationUrl;
    private readonly string _projectName;
    private readonly string _personalAccessToken;
    private readonly ISprintService _sprintService;

    public TaskCreator(string organizationUrl, string projectName, string personalAccessToken, ISprintService sprintService)
    {
        _existingTasks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _organizationUrl = organizationUrl ?? throw new ArgumentNullException(nameof(organizationUrl));
        _projectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        _personalAccessToken = personalAccessToken ?? throw new ArgumentNullException(nameof(personalAccessToken));
        _sprintService = sprintService ?? throw new ArgumentNullException(nameof(sprintService));
    }

    public async Task<bool> TaskExistsAsync(string board, string title)
    {

        using var httpClient = new HttpClient();
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_personalAccessToken}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

        var wiql = new
        {
            query = $@"
                SELECT [System.Id] FROM WorkItems
                WHERE [System.TeamProject] = '{_projectName}'
                AND [System.Title] = '{title.Replace("'", "''")}'
                AND [System.AreaPath] = '{board.Replace("'", "''")}'
            "
        };
        var wiqlJson = JsonSerializer.Serialize(wiql);
        var content = new StringContent(wiqlJson, Encoding.UTF8, "application/json");

        var url = $"{_organizationUrl}/{_projectName}/_apis/wit/wiql?api-version=7.0";
        var response = await httpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
            if (json.TryGetProperty("workItems", out var workItems) && workItems.GetArrayLength() > 0)
            {
                Console.WriteLine($"[Debug] Task '{title}' already exists in Azure DevOps.");
                _existingTasks.Add(title);
                return true;
            }
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Error] Failed to query for existing tasks: {response.StatusCode} - {error}");
        }

        Console.WriteLine($"[Debug] Task '{title}' does not exist in Azure DevOps.");
        return false;
    }

    public async Task<string?> CreateWorkItemAsync(string board, string title, string description, WorkItemType workItemType = WorkItemType.Task)
    {
        if (string.IsNullOrWhiteSpace(board))
        {
            throw new ArgumentNullException(nameof(board), "Board cannot be null or empty.");
        }

        if (await TaskExistsAsync(board, title))
        {
            Console.WriteLine($"[TaskCreator] Work item with title '{title}' already exists on board '{board}'. Skipping creation.");
            return null;
        }

        var teamName = board.Split('\\').Last();
        Console.WriteLine($"[Debug] Using team name: {teamName}");

        var currentSprint = await _sprintService.GetCurrentSprintAsync(teamName);

        Console.WriteLine($"[TaskCreator] Preparing to create {workItemType} on Azure DevOps board '{board}' in sprint '{currentSprint}'");
        Console.WriteLine($"Title: {title}");
        Console.WriteLine($"Description: {description}");

        var workItem = new[]
        {
            new { op = "add", path = "/fields/System.Title", value = title },
            new { op = "add", path = "/fields/System.Description", value = description },
            new { op = "add", path = "/fields/System.AreaPath", value = board },
            new { op = "add", path = "/fields/System.IterationPath", value = currentSprint }
        };

        var jsonContent = JsonSerializer.Serialize(workItem);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json-patch+json");

        using var httpClient = new HttpClient();
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_personalAccessToken}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

        var url = $"{_organizationUrl}/{_projectName}/_apis/wit/workitems/${workItemType.ToString()}?api-version=7.0";
        var response = await httpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[TaskCreator] {workItemType} created successfully. Response: {responseContent}");
            _existingTasks.Add(title);

            // Gebruik een model voor de response
            try
            {
                var workItemResponse = JsonSerializer.Deserialize<WorkItemResponse>(responseContent);
                if (workItemResponse != null && workItemResponse.id != null)
                {
                    return workItemResponse.id.ToString();
                }
            }
            catch { }
            return null;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Error] Failed to create {workItemType}: {response.StatusCode} - {error}");
            return null;
        }
    }

    public async Task<string?> GetWorkItemIdAsync(string board, string title)
    {
        using var httpClient = new HttpClient();
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_personalAccessToken}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

        var wiql = new
        {
            query = $@"
                SELECT [System.Id] FROM WorkItems
                WHERE [System.TeamProject] = '{_projectName}'
                AND [System.Title] = '{title.Replace("'", "''")}'
                AND [System.AreaPath] = '{board.Replace("'", "''")}'
            "
        };
        var wiqlJson = JsonSerializer.Serialize(wiql);
        var content = new StringContent(wiqlJson, Encoding.UTF8, "application/json");

        var url = $"{_organizationUrl}/{_projectName}/_apis/wit/wiql?api-version=7.0";
        var response = await httpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
            if (json.TryGetProperty("workItems", out var workItems) && workItems.GetArrayLength() > 0)
            {
                var firstWorkItem = workItems[0];
                if (firstWorkItem.TryGetProperty("id", out var idProp))
                {
                    return idProp.GetInt32().ToString();
                }
            }
        }
        return null;
    }

    public async Task<string?> GetWorkItemDescriptionAsync(string board, string title)
    {
        using var httpClient = new HttpClient();
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_personalAccessToken}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

        // Zoek het work item id
        var wiql = new
        {
            query = $@"
                SELECT [System.Id] FROM WorkItems
                WHERE [System.TeamProject] = '{_projectName}'
                AND [System.Title] = '{title.Replace("'", "''")}'
                AND [System.AreaPath] = '{board.Replace("'", "''")}'
            "
        };
        var wiqlJson = JsonSerializer.Serialize(wiql);
        var content = new StringContent(wiqlJson, Encoding.UTF8, "application/json");

        var url = $"{_organizationUrl}/{_projectName}/_apis/wit/wiql?api-version=7.0";
        var response = await httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
            return null;

        var responseContent = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
        if (json.TryGetProperty("workItems", out var workItems) && workItems.GetArrayLength() > 0)
        {
            var firstWorkItem = workItems[0];
            if (firstWorkItem.TryGetProperty("id", out var idProp))
            {
                var workItemId = idProp.GetInt32();
                // Haal de description op
                var getUrl = $"{_organizationUrl}/{_projectName}/_apis/wit/workitems/{workItemId}?fields=System.Description&api-version=7.0";
                var getResp = await httpClient.GetAsync(getUrl);
                if (!getResp.IsSuccessStatusCode)
                    return null;
                var getRespContent = await getResp.Content.ReadAsStringAsync();
                var getJson = JsonSerializer.Deserialize<JsonElement>(getRespContent);
                if (getJson.TryGetProperty("fields", out var fields) && fields.TryGetProperty("System.Description", out var descProp))
                {
                    return descProp.GetString();
                }
            }
        }
        return null;
    }

    public async Task UpdateWorkItemDescriptionAsync(string board, string title, string newDescription)
    {
        using var httpClient = new HttpClient();
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_personalAccessToken}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

        // Zoek het work item id
        var wiql = new
        {
            query = $@"
                SELECT [System.Id] FROM WorkItems
                WHERE [System.TeamProject] = '{_projectName}'
                AND [System.Title] = '{title.Replace("'", "''")}'
                AND [System.AreaPath] = '{board.Replace("'", "''")}'
            "
        };
        var wiqlJson = JsonSerializer.Serialize(wiql);
        var content = new StringContent(wiqlJson, Encoding.UTF8, "application/json");

        var url = $"{_organizationUrl}/{_projectName}/_apis/wit/wiql?api-version=7.0";
        var response = await httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
            return;

        var responseContent = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
        if (json.TryGetProperty("workItems", out var workItems) && workItems.GetArrayLength() > 0)
        {
            var firstWorkItem = workItems[0];
            if (firstWorkItem.TryGetProperty("id", out var idProp))
            {
                var workItemId = idProp.GetInt32();
                // Patch de description
                var patch = new[]
                {
                    new { op = "add", path = "/fields/System.Description", value = newDescription }
                };
                var patchContent = new StringContent(JsonSerializer.Serialize(patch), Encoding.UTF8, "application/json-patch+json");
                var patchUrl = $"{_organizationUrl}/{_projectName}/_apis/wit/workitems/{workItemId}?api-version=7.0";
                var patchResp = await httpClient.PatchAsync(patchUrl, patchContent);
                patchResp.EnsureSuccessStatusCode();
            }
        }
    }
}
