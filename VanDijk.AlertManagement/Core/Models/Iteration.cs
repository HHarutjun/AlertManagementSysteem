using System.Text.Json.Serialization;

/// <summary>
/// Represents an iteration with a path.
/// </summary>
public class Iteration
{
    /// <summary>
    /// Gets or sets the path of the iteration.
    /// </summary>
    [JsonPropertyName("path")]
    required public string Path { get; set; }
}