using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.Runtime;

/// <summary>
/// Provides log fetching functionality from AWS CloudWatch Logs.
/// </summary>
public class AWSLogProvider : ILogProvider
{
    private readonly AmazonCloudWatchLogsClient client;
    private readonly string logGroupName;

    /// <summary>
    /// Initializes a new instance of the <see cref="AWSLogProvider"/> class.
    /// </summary>
    /// <param name="logGroupName">The name of the AWS CloudWatch log group.</param>
    /// <param name="credentials">The AWS credentials to use for authentication.</param>
    /// <param name="region">The AWS region endpoint.</param>
    public AWSLogProvider(string logGroupName, AWSCredentials credentials, RegionEndpoint region)
    {
        this.logGroupName = logGroupName ?? throw new ArgumentNullException(nameof(logGroupName));
        this.client = new AmazonCloudWatchLogsClient(credentials, region);
    }

    /// <summary>
    /// Asynchronously fetches log messages from the specified AWS CloudWatch log group.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of log messages.</returns>
    public async Task<List<string>> FetchLogsAsync()
    {
        var logs = new List<string>();
        string? nextToken = null;

        do
        {
            var request = new FilterLogEventsRequest
            {
                LogGroupName = this.logGroupName,
                Limit = 50,
                NextToken = nextToken,
            };

            var response = await this.client.FilterLogEventsAsync(request);
            logs.AddRange(response.Events.Where(e => e.Message != null).Select(e => e.Message));
            nextToken = response.NextToken;
        }
        while (!string.IsNullOrEmpty(nextToken));
        return logs;
    }
}