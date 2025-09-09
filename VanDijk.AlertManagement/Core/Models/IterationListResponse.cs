using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a response containing a list of iterations.
/// </summary>
public class IterationListResponse
{
    /// <summary>
    /// Gets or sets the list of iterations.
    /// </summary>
    [JsonPropertyName("value")]
    public List<Iteration> Value { get; set; } = new List<Iteration>();
}
