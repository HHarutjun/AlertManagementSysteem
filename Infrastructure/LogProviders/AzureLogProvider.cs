using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure.Core;

public class AzureLogProvider : ILogProvider
{
    private readonly LogsQueryClient _logsClient;
    private readonly string _workspaceId;

    public AzureLogProvider(string workspaceId, TokenCredential credential)
    {
        _workspaceId = workspaceId;
        _logsClient = new LogsQueryClient(credential);
    }

    public async Task<List<string>> FetchLogsAsync()
    {
        const string appExceptionsQuery = "AppExceptions | take 300";
        var response = await _logsClient.QueryWorkspaceAsync(_workspaceId, appExceptionsQuery, TimeSpan.FromDays(1));

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
