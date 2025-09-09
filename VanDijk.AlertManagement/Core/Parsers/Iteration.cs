namespace VanDijk.AlertManagement.Core.Parsers
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an iteration (e.g., a sprint) in the alert management system.
    /// </summary>
    public class Iteration
    {
        /// <summary>
        /// Gets or sets the path of the iteration.
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }
}