using System;
using Amazon;
using Amazon.Runtime;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Factory class for creating log providers based on the specified provider type and configuration.
/// </summary>
public class LogProviderFactory
{
    /// <summary>
    /// Creates an instance of <see cref="ILogProvider"/> based on the specified provider type and configuration.
    /// </summary>
    /// <param name="providerType">The type of log provider to create.</param>
    /// <param name="config">The configuration containing necessary settings for the log provider.</param>
    /// <returns>An instance of <see cref="ILogProvider"/>.</returns>
    /// <exception cref="NotImplementedException">Thrown when the specified provider type is not implemented.</exception>
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
            config["Azure:ClientSecret"]);
        return new AzureLogProvider(workspaceId, credential);
    }

    private static ILogProvider CreateAWSLogProvider(IConfiguration config)
    {
        var logGroupName = config["AWS:LogGroupName"] ?? throw new ArgumentNullException("LogGroupName");
        var credentials = new BasicAWSCredentials(config["AWS:AccessKey"], config["AWS:SecretKey"]);
        return new AWSLogProvider(logGroupName, credentials, RegionEndpoint.USEast1);
    }
}
