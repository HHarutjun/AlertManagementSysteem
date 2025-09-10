using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VanDijk.AlertManagement.Core.Interfaces;

/// <summary>
/// Provides methods for creating and managing tasks (work items) in Azure DevOps.
/// </summary>
public class TaskCreator : ITaskCreator
{
    private readonly HashSet<string> existingTasks;
    private readonly string organizationUrl;
    private readonly string projectName;
    private readonly string personalAccessToken;
    private readonly ISprintService sprintService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskCreator"/> class.
    /// </summary>
    /// <param name="organizationUrl">The Azure DevOps organization URL.</param>
    /// <param name="projectName">The name of the Azure DevOps project.</param>
    /// <param name="personalAccessToken">The personal access token for authentication.</param>
    /// <param name="sprintService">The sprint service used to retrieve sprint information.</param>
    public TaskCreator(string organizationUrl, string projectName, string personalAccessToken, ISprintService sprintService)
    {
        this.existingTasks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        this.organizationUrl = organizationUrl ?? throw new ArgumentNullException(nameof(organizationUrl));
        this.projectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        this.personalAccessToken = personalAccessToken ?? throw new ArgumentNullException(nameof(personalAccessToken));
        this.sprintService = sprintService ?? throw new ArgumentNullException(nameof(sprintService));
    }

    /// <summary>
    /// Checks if a task with the specified title exists on the given Azure DevOps board.
    /// </summary>
    /// <param name="board">The name of the Azure DevOps board (area path).</param>
    /// <param name="title">The title of the task to check for existence.</param>
    /// <returns>True if the task exists; otherwise, false.</returns>
    public async Task<bool> TaskExistsAsync(string board, string title)
    {
        using var httpClient = this.CreateHttpClientWithAuth();
        var wiql = this.BuildWiqlQuery(title, board);
        var content = new StringContent(JsonSerializer.Serialize(wiql), Encoding.UTF8, "application/json");
        var url = $"{this.organizationUrl}/{this.projectName}/_apis/wit/wiql?api-version=7.0";
        var response = await httpClient.PostAsync(url, content);
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            if (this.HasWorkItems(responseContent))
            {
                Console.WriteLine($"[Debug] Task '{title}' already exists in Azure DevOps.");
                this.existingTasks.Add(title);
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

    /// <summary>
    /// Creates a new work item (task, bug, etc.) in Azure DevOps on the specified board with the given title and description.
    /// </summary>
    /// <param name="board">The name of the Azure DevOps board (area path).</param>
    /// <param name="title">The title of the work item to create.</param>
    /// <param name="description">The description of the work item.</param>
    /// <param name="workItemType">The type of work item to create (default is Task).</param>
    /// <returns>The ID of the created work item if successful; otherwise, null.</returns>
    public async Task<string?> CreateWorkItemAsync(string board, string title, string description, WorkItemType workItemType = WorkItemType.Task)
    {
        if (string.IsNullOrWhiteSpace(board))
        {
            throw new ArgumentNullException(nameof(board), "Board cannot be null or empty.");
        }

        if (await this.TaskExistsAsync(board, title))
        {
            Console.WriteLine($"[TaskCreator] Work item with title '{title}' already exists on board '{board}'. Skipping creation.");
            return null;
        }

        var teamName = board.Split('\\').Last();
        Console.WriteLine($"[Debug] Using team name: {teamName}");
        var currentSprint = await this.sprintService.GetCurrentSprintAsync(teamName);
        Console.WriteLine($"[TaskCreator] Preparing to create {workItemType} on Azure DevOps board '{board}' in sprint '{currentSprint}'");
        Console.WriteLine($"Title: {title}");
        Console.WriteLine($"Description: {description}");
        var workItemPatch = this.BuildWorkItemPatch(title, description, board, currentSprint);
        var jsonContent = JsonSerializer.Serialize(workItemPatch);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json-patch+json");
        using var httpClient = this.CreateHttpClientWithAuth();
        var url = $"{this.organizationUrl}/{this.projectName}/_apis/wit/workitems/${workItemType.ToString()}?api-version=7.0";
        var response = await httpClient.PostAsync(url, content);
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[TaskCreator] {workItemType} created successfully. Response: {responseContent}");
            this.existingTasks.Add(title);
            return this.ParseWorkItemIdFromResponse(responseContent);
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Error] Failed to create {workItemType}: {response.StatusCode} - {error}");
            return null;
        }
    }

    /// <summary>
    /// Retrieves the ID of a work item in Azure DevOps based on the specified board and title.
    /// </summary>
    /// <param name="board">The name of the Azure DevOps board (area path).</param>
    /// <param name="title">The title of the work item to search for.</param>
    /// <returns>The ID of the work item if found; otherwise, null.</returns>
    public async Task<string?> GetWorkItemIdAsync(string board, string title)
    {
        using var httpClient = this.CreateHttpClientWithAuth();
        var wiql = this.BuildWiqlQuery(title, board);
        var content = new StringContent(JsonSerializer.Serialize(wiql), Encoding.UTF8, "application/json");
        var url = $"{this.organizationUrl}/{this.projectName}/_apis/wit/wiql?api-version=7.0";
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

    /// <summary>
    /// Retrieves the description of a work item in Azure DevOps based on the specified board and title.
    /// </summary>
    /// <param name="board">The name of the Azure DevOps board (area path).</param>
    /// <param name="title">The title of the work item to search for.</param>
    /// <returns>The description of the work item if found; otherwise, null.</returns>
    public async Task<string?> GetWorkItemDescriptionAsync(string board, string title)
    {
        using var httpClient = this.CreateHttpClientWithAuth();
        var wiql = this.BuildWiqlQuery(title, board);
        var content = new StringContent(JsonSerializer.Serialize(wiql), Encoding.UTF8, "application/json");
        var url = $"{this.organizationUrl}/{this.projectName}/_apis/wit/wiql?api-version=7.0";
        var response = await httpClient.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
        if (json.TryGetProperty("workItems", out var workItems) && workItems.GetArrayLength() > 0)
        {
            var firstWorkItem = workItems[0];
            if (firstWorkItem.TryGetProperty("id", out var idProp))
            {
                var workItemId = idProp.GetInt32();
                var getUrl = $"{this.organizationUrl}/{this.projectName}/_apis/wit/workitems/{workItemId}?fields=System.Description&api-version=7.0";
                var getResp = await httpClient.GetAsync(getUrl);
                if (!getResp.IsSuccessStatusCode)
                {
                    return null;
                }

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

    /// <summary>
    /// Updates the description of a work item in Azure DevOps based on the specified board and title.
    /// </summary>
    /// <param name="board">The name of the Azure DevOps board (area path).</param>
    /// <param name="title">The title of the work item to update.</param>
    /// <param name="newDescription">The new description to set for the work item.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdateWorkItemDescriptionAsync(string board, string title, string newDescription)
    {
        using var httpClient = this.CreateHttpClientWithAuth();
        var wiql = this.BuildWiqlQuery(title, board);
        var content = new StringContent(JsonSerializer.Serialize(wiql), Encoding.UTF8, "application/json");
        var url = $"{this.organizationUrl}/{this.projectName}/_apis/wit/wiql?api-version=7.0";
        var response = await httpClient.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            return;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
        if (json.TryGetProperty("workItems", out var workItems) && workItems.GetArrayLength() > 0)
        {
            var firstWorkItem = workItems[0];
            if (firstWorkItem.TryGetProperty("id", out var idProp))
            {
                var workItemId = idProp.GetInt32();
                var patch = new[]
                {
                    new { op = "add", path = "/fields/System.Description", value = newDescription },
                };
                var patchContent = new StringContent(JsonSerializer.Serialize(patch), Encoding.UTF8, "application/json-patch+json");
                var patchUrl = $"{this.organizationUrl}/{this.projectName}/_apis/wit/workitems/{workItemId}?api-version=7.0";
                var patchResp = await httpClient.PatchAsync(patchUrl, patchContent);
                patchResp.EnsureSuccessStatusCode();
            }
        }
    }

    private HttpClient CreateHttpClientWithAuth()
    {
        var httpClient = new HttpClient();
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{this.personalAccessToken}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
        return httpClient;
    }

    private object BuildWiqlQuery(string title, string board)
    {
        return new
        {
            query = $@"
                SELECT [System.Id] FROM WorkItems
                WHERE [System.TeamProject] = '{this.projectName}'
                AND [System.Title] = '{title.Replace("'", "''")}'
                AND [System.AreaPath] = '{board.Replace("'", "''")}'
            ",
        };
    }

    private object[] BuildWorkItemPatch(string title, string description, string board, string currentSprint)
    {
        return new[]
        {
            new { op = "add", path = "/fields/System.Title", value = title },
            new { op = "add", path = "/fields/System.Description", value = description },
            new { op = "add", path = "/fields/System.AreaPath", value = board },
            new { op = "add", path = "/fields/System.IterationPath", value = currentSprint },
        };
    }

    private bool HasWorkItems(string responseContent)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
        return json.TryGetProperty("workItems", out var workItems) && workItems.GetArrayLength() > 0;
    }

    private string? ParseWorkItemIdFromResponse(string responseContent)
    {
        try
        {
            var workItemResponse = JsonSerializer.Deserialize<WorkItemResponse>(responseContent);
            if (workItemResponse != null && workItemResponse.Id != null)
            {
                return workItemResponse.Id.ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to parse WorkItemResponse: {ex.Message}");
        }

        return null;
    }
}
