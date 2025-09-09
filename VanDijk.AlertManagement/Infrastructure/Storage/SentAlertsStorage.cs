namespace Infrastructure.Storage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;

    /// <summary>
    /// Provides functionality to load and save sent alerts to a file.
    /// </summary>
    public class SentAlertsStorage : ISentAlertsStorage
    {
        private readonly string filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentAlertsStorage"/> class.
        /// </summary>
        /// <param name="filePath">The file path where sent alerts are stored.</param>
        public SentAlertsStorage(string filePath)
        {
            this.filePath = filePath;
        }

        /// <summary>
        /// Loads the set of sent alerts from the storage file.
        /// </summary>
        /// <returns>A set of alert identifiers that have been sent.</returns>
        public ISet<string> LoadSentAlerts()
        {
            if (!File.Exists(this.filePath))
            {
                Console.WriteLine($"[Debug] sent_alerts.json does not exist, returning empty set.");
                return new HashSet<string>();
            }

            var json = File.ReadAllText(this.filePath);
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

        /// <summary>
        /// Saves the set of sent alerts to the storage file.
        /// </summary>
        /// <param name="sentAlerts">A set of alert identifiers that have been sent.</param>
        public void SaveSentAlerts(ISet<string> sentAlerts)
        {
            var directory = Path.GetDirectoryName(this.filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(sentAlerts);
            File.WriteAllText(this.filePath, json);
        }
    }
}