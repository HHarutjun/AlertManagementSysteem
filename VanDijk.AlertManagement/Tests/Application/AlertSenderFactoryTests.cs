using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Xunit;

public class AlertSenderFactoryTests
{
    /// <summary>
    /// AUT-21: Test aanmaken van FlowMailerAlertSender.
    /// </summary>
    [Fact(DisplayName = "AUT-21: Maakt FlowMailerAlertSender aan bij type Email")]
    public void CreateAlertSender_Email_ReturnsFlowMailerAlertSender()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string>
        {
            ["AlertSettings:FlowMailer:ApiEndpoint"] = "api",
            ["AlertSettings:FlowMailer:OAuthEndpoint"] = "oauth",
            ["AlertSettings:FlowMailer:ClientId"] = "id",
            ["AlertSettings:FlowMailer:ClientSecret"] = "secret",
            ["AlertSettings:FlowMailer:AccountId"] = "acc"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var factory = new AlertSenderFactory(config);

        // Act
        var sender = factory.CreateAlertSender(AlertChannelType.Email);

        // Assert
        Assert.NotNull(sender);
        Assert.IsType<FlowMailerAlertSender>(sender);
    }

    /// <summary>
    /// AUT-22: Test exception bij onbekend alert type.
    /// </summary>
    [Fact(DisplayName = "AUT-22: Gooit NotImplementedException bij onbekend type")]
    public void CreateAlertSender_UnknownType_Throws()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var factory = new AlertSenderFactory(config);

        // Act & Assert
        Assert.Throws<NotImplementedException>(() => factory.CreateAlertSender((AlertChannelType)999));
    }
}
