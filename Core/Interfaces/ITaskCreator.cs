using System.Threading.Tasks;

public interface ITaskCreator
{
    Task<bool> TaskExistsAsync(string board, string title);
    Task<string?> CreateWorkItemAsync(string board, string title, string description, WorkItemType workItemType = WorkItemType.Task);
    Task<string?> GetWorkItemDescriptionAsync(string board, string title);
    Task UpdateWorkItemDescriptionAsync(string board, string title, string newDescription);
}
