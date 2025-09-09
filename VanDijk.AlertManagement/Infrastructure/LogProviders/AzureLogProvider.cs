using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;

/// <summary>
/// Provides functionality to fetch logs from Azure Monitor using a specified workspace and credentials.
/// </summary>
public class AzureLogProvider : ILogProvider
{
    private readonly LogsQueryClient logsClient;
    private readonly string workspaceId;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureLogProvider"/> class.
    /// </summary>
    /// <param name="workspaceId">The Azure Log Analytics workspace ID.</param>
    /// <param name="credential">The Azure credential used for authentication.</param>
    public AzureLogProvider(string workspaceId, TokenCredential credential)
    {
        this.workspaceId = workspaceId;
        this.logsClient = new LogsQueryClient(credential);
    }

    /// <summary>
    /// Asynchronously fetches logs from the Azure Log Analytics workspace.
    /// </summary>
    /// <returns>A list of log entries as strings.</returns>
    public async Task<List<string>> FetchLogsAsync()
    {
        const string appExceptionsQuery = "AppExceptions | take 300";
        var response = await this.logsClient.QueryWorkspaceAsync(this.workspaceId, appExceptionsQuery, TimeSpan.FromDays(1));

        var logs = new List<string>();

        foreach (var row in response.Value.Table.Rows)
        {
            var component = row["AppRoleName"]?.ToString() ?? "No AppRoleName";
            var severity = row["SeverityLevel"]?.ToString() ?? "No Severity";
            var timestamp = row["TimeGenerated"]?.ToString() ?? "No TimeGenerated";
            var problemId = row["ProblemId"]?.ToString() ?? "No ProblemId";
            var exceptionType = row["ExceptionType"]?.ToString() ?? "No ExceptionType";

            var logEntry = $"Timestamp: {timestamp} | Component: {component} | Severity: {severity} | ProblemId: {problemId} | ExceptionType: {exceptionType}";
            Console.WriteLine(logEntry);
            logs.Add(logEntry);
        }

        return logs;
    }
}
