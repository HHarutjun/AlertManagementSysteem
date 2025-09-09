using System.Threading.Tasks;

/// <summary>
/// Provides methods for creating and managing work items.
/// </summary>
public interface ITaskCreator
{
    /// <summary>
    /// Checks if a task exists on the specified board with the given title.
    /// </summary>
    /// <param name="board">The board to search.</param>
    /// <param name="title">The title of the task.</param>
    /// <returns>True if the task exists; otherwise, false.</returns>
    Task<bool> TaskExistsAsync(string board, string title);

    /// <summary>
    /// Creates a new work item on the specified board.
    /// </summary>
    /// <param name="board">The board to create the work item on.</param>
    /// <param name="title">The title of the work item.</param>
    /// <param name="description">The description of the work item.</param>
    /// <param name="workItemType">The type of the work item.</param>
    /// <returns>The ID of the created work item, or null if creation failed.</returns>
    Task<string?> CreateWorkItemAsync(string board, string title, string description, WorkItemType workItemType = WorkItemType.Task);

    /// <summary>
    /// Gets the description of a work item by board and title.
    /// </summary>
    /// <param name="board">The board containing the work item.</param>
    /// <param name="title">The title of the work item.</param>
    /// <returns>The description of the work item, or null if not found.</returns>
    Task<string?> GetWorkItemDescriptionAsync(string board, string title);

    /// <summary>
    /// Updates the description of a work item.
    /// </summary>
    /// <param name="board">The board containing the work item.</param>
    /// <param name="title">The title of the work item.</param>
    /// <param name="newDescription">The new description to set.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateWorkItemDescriptionAsync(string board, string title, string newDescription);
}
