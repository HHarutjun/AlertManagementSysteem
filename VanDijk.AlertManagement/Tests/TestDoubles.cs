using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using VanDijk.AlertManagement.Core.Interfaces;


// In-memory log provider for integration tests
public class InMemoryLogProvider : ILogProvider
{
    private readonly List<string> _logs;
    public InMemoryLogProvider(List<string> logs) => _logs = logs;
    public Task<List<string>> FetchLogsAsync() => Task.FromResult(_logs);
}

// In-memory sent alerts storage for integration tests
public class InMemorySentAlertsStorage : ISentAlertsStorage
{
    private readonly HashSet<string> _sent = new();
    public ISet<string> LoadSentAlerts() => _sent;
    public void SaveSentAlerts(ISet<string> sentAlerts) { }
}

// Test alert sender for integration tests
public class TestAlertSender : IAlertSender
{
    public List<(string Message, string RecipientEmail, string Component)> SentAlerts { get; } = new();
    public Task SendAlertAsync(string message, string recipientEmail, string component)
    {
        SentAlerts.Add((message, recipientEmail, component));
        return Task.CompletedTask;
    }
}

// Test alert sender for Teams integration
public class TestTeamsAlertSender : IAlertSender
{
    public List<(string Message, string RecipientEmail, string Component)> SentTeamsAlerts { get; } = new();
    public Task SendAlertAsync(string message, string recipientEmail, string component)
    {
        SentTeamsAlerts.Add((message, recipientEmail, component));
        return Task.CompletedTask;
    }
}

// Test task creator for integration tests
public class TestTaskCreator : ITaskCreator
{
    public Task<bool> TaskExistsAsync(string board, string title) => Task.FromResult(false);
    public Task<string?> CreateWorkItemAsync(string board, string title, string description, WorkItemType workItemType = WorkItemType.Task)
    {
        // Simuleer aanmaken van een werkitem, geef eventueel een dummy id terug
        return Task.FromResult<string?>("TestWorkItemId");
    }

    public Task<string?> GetWorkItemDescriptionAsync(string board, string title)
    {
        // Return a dummy description for testing purposes
        return Task.FromResult<string?>("Dummy description");
    }

    public Task UpdateWorkItemDescriptionAsync(string board, string title, string newDescription)
    {
        // Simulate updating the description (no-op for test double)
        return Task.CompletedTask;
    }
}

// Switchable log provider for config change integration tests
public class SwitchableLogProvider : ILogProvider
{
    private List<string> _logs;
    public SwitchableLogProvider(List<string> logs) => _logs = logs;
    public void SetLogs(List<string> logs) => _logs = logs;
    public Task<List<string>> FetchLogsAsync() => Task.FromResult(_logs);
}

public class TestTaskCreatorWithTracking : ITaskCreator
{
    public List<(string Board, string Title, string Description, string WorkItemType)> CreatedWorkItems { get; } = new();
    public Task<bool> TaskExistsAsync(string board, string title)
    {
        var exists = CreatedWorkItems.Any(w => w.Board == board && w.Title == title);
        return Task.FromResult(exists);
    }
    public Task<string?> CreateWorkItemAsync(string board, string title, string description, WorkItemType workItemType = WorkItemType.Task)
    {
        if (CreatedWorkItems.Any(w => w.Board == board && w.Title == title))
            return Task.FromResult<string?>(null);
        CreatedWorkItems.Add((board, title, description, workItemType.ToString()));
        return Task.FromResult<string?>("TrackedWorkItemId");
    }

    public Task<string?> GetWorkItemDescriptionAsync(string board, string title)
    {
        // Return a dummy description for testing purposes
        return Task.FromResult<string?>("Dummy description");
    }

    public Task UpdateWorkItemDescriptionAsync(string board, string title, string newDescription)
    {
        // Simulate updating the description (no-op for test double)
        return Task.CompletedTask;
    }
}
    
public class TestEmailAlertSender : IAlertSender
{
    public List<(string Message, string RecipientEmail, string Component)> SentEmails { get; } = new();
    public Task SendAlertAsync(string message, string recipientEmail, string component)
    {
        SentEmails.Add((message, recipientEmail, component));
        return Task.CompletedTask;
    }
}

public class FailingEmailAlertSender : IAlertSender
{
    public Task SendAlertAsync(string message, string recipientEmail, string component)
    {
        throw new Exception("SMTP server unreachable");
    }
}

public class FailingTaskCreator : ITaskCreator
{
    public Task<bool> TaskExistsAsync(string board, string title) => Task.FromResult(false);
    public Task<string?> CreateWorkItemAsync(string board, string title, string description, WorkItemType workItemType = WorkItemType.Task)
    {
        // Simuleer een failure
        throw new Exception("Simulated failure in CreateWorkItemAsync");
    }

    public Task<string?> GetWorkItemDescriptionAsync(string board, string title)
    {
        throw new Exception("Simulated failure in GetWorkItemDescriptionAsync");
    }

    public Task UpdateWorkItemDescriptionAsync(string board, string title, string newDescription)
    {
        throw new Exception("Simulated failure in UpdateWorkItemDescriptionAsync");
    }
}

// Dummy SprintService voor unit tests
public class SprintServiceFake : ISprintService
{
    private readonly HttpClient _httpClient;
    public SprintServiceFake(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<string> GetCurrentSprintAsync(string teamName)
    {
        var response = await _httpClient.GetAsync("http://fake-url");
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var iterationList = System.Text.Json.JsonSerializer.Deserialize<IterationListResponse>(responseContent);
            // Debug: print de response en het resultaat van de deserialisatie
            Console.WriteLine("[Test] Raw response: " + responseContent);
            Console.WriteLine("[Test] IterationListResponse.Value.Count: " + (iterationList?.Value?.Count ?? -1));
            if (iterationList?.Value != null && iterationList.Value.Count > 0)
            {
                var currentIteration = iterationList.Value[0];
                Console.WriteLine("[Test] Iteration.Path: " + currentIteration.Path);
                if (!string.IsNullOrEmpty(currentIteration.Path))
                    return currentIteration.Path;
                throw new Exception("Iteration path is null.");
            }
            throw new Exception("No current sprint found for the team.");
        }
        else
        {
            throw new Exception($"Failed to fetch current sprint: {response.StatusCode}");
        }
    }
}

// Dummy voor deserialisatie van Azure DevOps iterations
public class IterationListResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public System.Collections.Generic.List<Iteration> Value { get; set; }
}
public class Iteration
{
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; }
}


public class InMemoryRecipientStorage : RecipientStorage
{
    private List<Recipient> _recipients;
    public InMemoryRecipientStorage(List<Recipient> recipients) : base("dummy.json")
    {
        _recipients = recipients ?? new List<Recipient>();
    }
    public override List<Recipient> LoadRecipients() => _recipients;
    public void SetRecipients(List<Recipient> recipients) => _recipients = recipients ?? new List<Recipient>();
}

public class CapturingTaskCreator : ITaskCreator
{
    public List<string> CreatedTitles { get; } = new();
    public Task<bool> TaskExistsAsync(string board, string title) => Task.FromResult(false);
    public Task<string?> CreateWorkItemAsync(string board, string title, string description, WorkItemType workItemType = WorkItemType.Task)
    {
        System.Diagnostics.Debug.WriteLine($"[TEST] CreateWorkItemAsync called with title: {title}");
        CreatedTitles.Add(title);
        return Task.FromResult<string?>("TestWorkItemId");
    }

    public Task<string?> GetWorkItemDescriptionAsync(string board, string title)
    {
        // Return a dummy description for testing purposes
        return Task.FromResult<string?>("Dummy description");
    }

    public Task UpdateWorkItemDescriptionAsync(string board, string title, string newDescription)
    {
        // Simulate updating the description (no-op for test double)
        return Task.CompletedTask;
    }
}
public class TestRecipientRoutingStrategy : RecipientRoutingStrategy
{
    private readonly List<Recipient> _recipients;
    public TestRecipientRoutingStrategy(List<Recipient> recipients) : base(recipients)
    {
        _recipients = recipients;
    }
    public override IEnumerable<Recipient> GetRecipientsForComponent(string component)
    {
        return _recipients;
    }
}
public class TestTaskCreatorWithDescriptionUpdate : ITaskCreator
{
    private readonly HashSet<string> _createdTitles = new();
    private readonly Dictionary<string, string> _descriptions = new();
    public Dictionary<string, string> Descriptions => _descriptions;


    public Task<bool> TaskExistsAsync(string board, string title)
    {
        return Task.FromResult(_createdTitles.Contains(title));
    }

    public Task<string?> CreateWorkItemAsync(string board, string title, string description, WorkItemType workItemType = WorkItemType.Task)
    {
        _createdTitles.Add(title);
        if (!_descriptions.ContainsKey(title))
            _descriptions[title] = "";
        _descriptions[title] = MergeProblemIds(_descriptions[title], description);
        return Task.FromResult<string?>("TestWorkItemId");
    }

    public Task<string?> GetWorkItemDescriptionAsync(string board, string title)
    {
        if (_descriptions.TryGetValue(title, out var desc))
            return Task.FromResult<string?>(desc);
        return Task.FromResult<string?>(null);
    }

    public Task UpdateWorkItemDescriptionAsync(string board, string title, string newDescription)
    {
        if (!_descriptions.ContainsKey(title))
            _descriptions[title] = "";
        _descriptions[title] = MergeProblemIds(_descriptions[title], newDescription);
        return Task.CompletedTask;
    }

    private string MergeProblemIds(string oldDesc, string newDesc)
    {
        var allProblemIds = new HashSet<string>();
        // Add existing problemIds from previous description
        if (!string.IsNullOrWhiteSpace(oldDesc))
        {
            foreach (var line in oldDesc.Split('\n'))
            {
                if (line.Contains("ProblemId:"))
                {
                    var pid = line.Split("ProblemId:").Last().Trim();
                    if (!string.IsNullOrWhiteSpace(pid))
                        allProblemIds.Add(pid);
                }
            }
        }
        // Add new problemIds from the incoming description
        if (!string.IsNullOrWhiteSpace(newDesc))
        {
            foreach (var line in newDesc.Split('\n'))
            {
                if (line.Contains("ProblemId:"))
                {
                    var pid = line.Split("ProblemId:").Last().Trim();
                    if (!string.IsNullOrWhiteSpace(pid))
                        allProblemIds.Add(pid);
                }
            }
        }
        // Compose the new description with all unique problemIds
        return string.Join("\n", allProblemIds.Select(pid => $"ProblemId: {pid}"));
    }
}