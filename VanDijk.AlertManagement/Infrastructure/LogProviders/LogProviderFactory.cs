using System;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Amazon.Runtime;
using Amazon;

public class LogProviderFactory
{
    public static ILogProvider CreateLogProvider(LogProviderType providerType, IConfiguration config)
    {
        return providerType switch
        {
            LogProviderType.Azure => CreateAzureLogProvider(config),
            LogProviderType.AWS => CreateAWSLogProvider(config),
            _ => throw new NotImplementedException($"Log provider {providerType} is not implemented.")
        };
    }

    private static ILogProvider CreateAzureLogProvider(IConfiguration config)
    {
        var workspaceId = config["Azure:WorkspaceId"] ?? throw new ArgumentNullException("WorkspaceId");
        var credential = new ClientSecretCredential(
            config["Azure:TenantId"],
            config["Azure:ClientId"],
            config["Azure:ClientSecret"]
        );
        return new AzureLogProvider(workspaceId, credential);
    }

    private static ILogProvider CreateAWSLogProvider(IConfiguration config)
    {
        var logGroupName = config["AWS:LogGroupName"] ?? throw new ArgumentNullException("LogGroupName");
        var credentials = new BasicAWSCredentials(config["AWS:AccessKey"], config["AWS:SecretKey"]);
        return new AWSLogProvider(logGroupName, credentials, RegionEndpoint.USEast1);
    }
}
