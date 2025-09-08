using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public class RecipientStorage
{
    private readonly string _filePath;

    public RecipientStorage(string filePath)
    {
        _filePath = filePath;
    }

    public virtual List<Recipient> LoadRecipients()
    {
        if (!File.Exists(_filePath))
            return new List<Recipient>();
        var json = File.ReadAllText(_filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
        };
        return JsonSerializer.Deserialize<List<Recipient>>(json, options) ?? new List<Recipient>();
    }

    public void SaveRecipients(List<Recipient> recipients)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        var json = JsonSerializer.Serialize(recipients, options);
        File.WriteAllText(_filePath, json);
    }
}
