using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System;

namespace Infrastructure.Storage
{
    public class SentAlertsStorage : ISentAlertsStorage
    {
        private readonly string _filePath;

        public SentAlertsStorage(string filePath)
        {
            _filePath = filePath;
        }

        public ISet<string> LoadSentAlerts()
        {
            if (!File.Exists(_filePath))
            {
                Console.WriteLine($"[Debug] sent_alerts.json does not exist, returning empty set.");
                return new HashSet<string>();
            }

            var json = File.ReadAllText(_filePath);
            Console.WriteLine($"[Debug] sent_alerts.json content: '{json}'");
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine("[Debug] sent_alerts.json is empty, returning empty set.");
                return new HashSet<string>();
            }
            var set = JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
            Console.WriteLine($"[Debug] sent_alerts loaded: [{string.Join(", ", set)}]");
            return set;
        }

        public void SaveSentAlerts(ISet<string> sentAlerts)
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(sentAlerts);
            File.WriteAllText(_filePath, json);
        }
    }
}