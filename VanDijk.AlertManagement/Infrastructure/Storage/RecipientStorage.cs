using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Provides methods to load and save Recipient objects to a file in JSON format.
/// </summary>
public class RecipientStorage
{
    private readonly string filePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecipientStorage"/> class with the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the file where recipients are stored.</param>
    public RecipientStorage(string filePath)
    {
        this.filePath = filePath;
    }

    /// <summary>
    /// Loads the list of recipients from the file in JSON format.
    /// </summary>
    /// <returns>A list of <see cref="Recipient"/> objects loaded from the file, or an empty list if the file does not exist.</returns>
    public virtual List<Recipient> LoadRecipients()
    {
        if (!File.Exists(this.filePath))
        {
            return new List<Recipient>();
        }

        var json = File.ReadAllText(this.filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true), },
        };
        return JsonSerializer.Deserialize<List<Recipient>>(json, options) ?? new List<Recipient>();
    }

    /// <summary>
    /// Saves the list of recipients to the file in JSON format.
    /// </summary>
    /// <param name="recipients">The list of Recipient objects to save.</param>
    public void SaveRecipients(List<Recipient> recipients)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() },
            };
            var json = JsonSerializer.Serialize(recipients, options);
            File.WriteAllText(this.filePath, json);
        }
}
