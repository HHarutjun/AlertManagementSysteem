using System;
using System.Threading.Tasks;
using Moq;
using VanDijk.AlertManagement.Core.Interfaces;
using Xunit;

public class TaskCreatorTests
{
    /// <summary>
    /// AUT-32: Test TaskExistsAsync true bij bestaande taak (mocked)
    /// </summary>
    [Fact(DisplayName = "AUT-32: TaskExistsAsync retourneert true bij bestaande taak")]
    public async Task TaskExistsAsync_ReturnsTrue_WhenTaskExists()
    {
        var sprintService = new Mock<ISprintService>();
        var creator = new TaskCreatorFake(true, sprintService.Object);

        var result = await creator.TaskExistsAsync("Board", "Title");

        Assert.True(result);
    }

    /// <summary>
    /// AUT-33: Test TaskExistsAsync false bij niet-bestaande taak (mocked)
    /// </summary>
    [Fact(DisplayName = "AUT-33: TaskExistsAsync retourneert false bij niet-bestaande taak")]
    public async Task TaskExistsAsync_ReturnsFalse_WhenTaskDoesNotExist()
    {
        var sprintService = new Mock<ISprintService>();
        var creator = new TaskCreatorFake(false, sprintService.Object);

        var result = await creator.TaskExistsAsync("Board", "Title");

        Assert.False(result);
    }

    /// <summary>
    /// AUT-34: Test CreateWorkItemAsync maakt werkitem aan (mocked)
    /// </summary>
    [Fact(DisplayName = "AUT-34: CreateWorkItemAsync maakt werkitem aan")]
    public async Task CreateWorkItemAsync_CreatesWorkItem_WhenNotExists()
    {
        var sprintService = new Mock<ISprintService>();
        var creator = new TaskCreatorFake(false, sprintService.Object);

        await creator.CreateWorkItemAsync("Board", "Title", "Desc", WorkItemType.Bug);

        Assert.Single(creator.CreatedWorkItems);
        Assert.Equal("Title", creator.CreatedWorkItems[0].Title);
    }

    /// <summary>
    /// AUT-35: Test CreateWorkItemAsync doet niets bij bestaande taak
    /// </summary>
    [Fact(DisplayName = "AUT-35: CreateWorkItemAsync doet niets bij bestaande taak")]
    public async Task CreateWorkItemAsync_DoesNothing_WhenTaskExists()
    {
        var sprintService = new Mock<ISprintService>();
        var creator = new TaskCreatorFake(true, sprintService.Object);

        await creator.CreateWorkItemAsync("Board", "Title", "Desc", WorkItemType.Bug);

        Assert.Empty(creator.CreatedWorkItems);
    }

    /// <summary>
    /// AUT-36: Test exception bij lege board in CreateWorkItemAsync
    /// </summary>
    [Fact(DisplayName = "AUT-36: CreateWorkItemAsync gooit ArgumentNullException bij lege board")]
    public async Task CreateWorkItemAsync_Throws_WhenBoardIsEmpty()
    {
        var sprintService = new Mock<ISprintService>();
        var creator = new TaskCreatorFake(false, sprintService.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            creator.CreateWorkItemAsync("", "Title", "Desc", WorkItemType.Bug));
    }
}

// Fake TaskCreator voor unit tests
public class TaskCreatorFake : ITaskCreator
{
    private readonly bool _taskExists;
    public System.Collections.Generic.List<(string Board, string Title, string Description, string WorkItemType)> CreatedWorkItems { get; } = new();
    private readonly ISprintService _sprintService;

    public TaskCreatorFake(bool taskExists, ISprintService sprintService)
    {
        _taskExists = taskExists;
        _sprintService = sprintService;
    }

    public Task<bool> TaskExistsAsync(string board, string title) => Task.FromResult(_taskExists);

    public Task<string?> CreateWorkItemAsync(string board, string title, string description, WorkItemType workItemType = WorkItemType.Task)
    {
        if (string.IsNullOrWhiteSpace(board))
            throw new ArgumentNullException(nameof(board));
        if (_taskExists)
            return Task.FromResult<string?>(null);
        CreatedWorkItems.Add((board, title, description, workItemType.ToString()));
        return Task.FromResult<string?>("123");
    }

    public Task<string?> GetWorkItemDescriptionAsync(string board, string title)
    {
        // Return the description if found, otherwise null
        var item = CreatedWorkItems.Find(w => w.Board == board && w.Title == title);
        return Task.FromResult<string?>(item.Description);
    }

    public Task UpdateWorkItemDescriptionAsync(string board, string title, string newDescription)
    {
        // Update the description if found
        var index = CreatedWorkItems.FindIndex(w => w.Board == board && w.Title == title);
        if (index >= 0)
        {
            var item = CreatedWorkItems[index];
            CreatedWorkItems[index] = (item.Board, item.Title, newDescription, item.WorkItemType);
        }
        return Task.CompletedTask;
    }
}
