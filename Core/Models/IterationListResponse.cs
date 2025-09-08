using System.Collections.Generic;
using System.Text.Json.Serialization;

public class IterationListResponse
{
    [JsonPropertyName("value")]
    public List<Iteration> Value { get; set; }
}

public class Iteration
{
    [JsonPropertyName("path")]
    public string Path { get; set; }
}
