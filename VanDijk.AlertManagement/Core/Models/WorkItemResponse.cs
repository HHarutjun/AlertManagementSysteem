/// <summary>
/// Represents a response for a work item, containing its ID and URL.
/// </summary>
public class WorkItemResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the work item.
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// Gets or sets the URL of the work item.
    /// </summary>
    public string? Url { get; set; }
}
