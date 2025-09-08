using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Factory class for creating alert sender instances based on the alert channel type.
/// </summary>
public class AlertSenderFactory
{
    private readonly IConfiguration configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlertSenderFactory"/> class.
    /// </summary>
    /// <param name="configuration">The configuration instance used to retrieve alert settings.</param>
    public AlertSenderFactory(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>
    /// Parses a string value to its corresponding <see cref="AlertChannelType"/>.
    /// </summary>
    /// <param name="value">The string representation of the alert channel type.</param>
    /// <returns>The parsed <see cref="AlertChannelType"/> value.</returns>
    /// <exception cref="ArgumentException">Thrown when the value does not match any valid alert channel type.</exception>
    public static AlertChannelType ParseAlertChannelType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AlertChannelType.Email;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "email" => AlertChannelType.Email,
            "teams" => AlertChannelType.Teams,
            "both" => AlertChannelType.Both,
            "smtp" => AlertChannelType.Smtp,
            _ => throw new ArgumentException($"Invalid AlertType '{value}'. Allowed values: Email, Teams, Smtp, Both.")
        };
    }

    /// <summary>
    /// Creates an alert sender instance based on the specified alert channel type.
    /// </summary>
    /// <param name="alertType">The type of alert channel for which to create the sender.</param>
    /// <returns>An instance of <see cref="IAlertSender"/> appropriate for the specified alert channel type.</returns>
    public IAlertSender CreateAlertSender(AlertChannelType alertType)
    {
        switch (alertType)
        {
            case AlertChannelType.Email:
                var apiEndpoint = this.configuration["AlertSettings:FlowMailer:ApiEndpoint"] ?? throw new ArgumentNullException("ApiEndpoint");
                var oauthEndpoint = this.configuration["AlertSettings:FlowMailer:OAuthEndpoint"] ?? throw new ArgumentNullException("OAuthEndpoint");
                var clientId = this.configuration["AlertSettings:FlowMailer:ClientId"] ?? throw new ArgumentNullException("ClientId");
                var clientSecret = this.configuration["AlertSettings:FlowMailer:ClientSecret"] ?? throw new ArgumentNullException("ClientSecret");
                var accountId = this.configuration["AlertSettings:FlowMailer:AccountId"] ?? throw new ArgumentNullException("AccountId");
                return new FlowMailerAlertSender(apiEndpoint, oauthEndpoint, clientId, clientSecret, accountId);

            case AlertChannelType.Teams:
                var tenantId = this.configuration["AlertSettings:Teams:TenantId"] ?? throw new ArgumentNullException("TenantId");
                var clientIdTeams = this.configuration["AlertSettings:Teams:ClientId"] ?? throw new ArgumentNullException("ClientId");
                var clientSecretTeams = this.configuration["AlertSettings:Teams:ClientSecret"] ?? throw new ArgumentNullException("ClientSecret");
                return new TeamsUserAlertSender(tenantId, clientIdTeams, clientSecretTeams);

            case AlertChannelType.Both:
                var senders = new List<IAlertSender>
                {
                    this.CreateAlertSender(AlertChannelType.Email),
                    this.CreateAlertSender(AlertChannelType.Teams),
                };
                return new MultiAlertSender(senders);

            // SMTP integratie
            case AlertChannelType.Smtp:
                var smtpHost = this.configuration["AlertSettings:Smtp:Host"] ?? throw new ArgumentNullException("SmtpHost");
                var smtpPort = int.Parse(this.configuration["AlertSettings:Smtp:Port"] ?? "587");
                var smtpUser = this.configuration["AlertSettings:Smtp:User"] ?? throw new ArgumentNullException("SmtpUser");
                var smtpPass = this.configuration["AlertSettings:Smtp:Pass"] ?? throw new ArgumentNullException("SmtpPass");
                var fromEmail = this.configuration["AlertSettings:Smtp:From"] ?? throw new ArgumentNullException("SmtpFrom");
                return new SmtpAlertSender(smtpHost, smtpPort, smtpUser, smtpPass, fromEmail);

            default:
                throw new NotImplementedException($"Alert type {alertType} not implemented.");
        }
    }
}
