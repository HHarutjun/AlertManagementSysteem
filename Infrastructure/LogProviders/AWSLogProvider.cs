using Amazon.CloudWatchLogs.Model;
using Amazon.CloudWatchLogs;
using Amazon.Runtime;
using Amazon;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

public class AWSLogProvider : ILogProvider
{
    private readonly AmazonCloudWatchLogsClient _client;
    private readonly string _logGroupName;

    public AWSLogProvider(string logGroupName, AWSCredentials credentials, RegionEndpoint region)
    {
        _logGroupName = logGroupName ?? throw new ArgumentNullException(nameof(logGroupName));
        _client = new AmazonCloudWatchLogsClient(credentials, region);
    }

    public async Task<List<string>> FetchLogsAsync()
    {
        var logs = new List<string>();
        string nextToken = null;

        do
        {
            var request = new FilterLogEventsRequest
            {
                LogGroupName = _logGroupName,
                Limit = 50,
                NextToken = nextToken
            };

            var response = await _client.FilterLogEventsAsync(request);
            logs.AddRange(response.Events.Where(e => e.Message != null).Select(e => e.Message));
            nextToken = response.NextToken;

        } while (!string.IsNullOrEmpty(nextToken));

        return logs;
    }
}