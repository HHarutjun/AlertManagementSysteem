using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

public class AlertSenderFactory
{
    private readonly IConfiguration _configuration;

    public AlertSenderFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IAlertSender CreateAlertSender(AlertChannelType alertType)
    {
        switch (alertType)
        {
            case AlertChannelType.Email:
                var apiEndpoint = _configuration["AlertSettings:FlowMailer:ApiEndpoint"] ?? throw new ArgumentNullException("ApiEndpoint");
                var oauthEndpoint = _configuration["AlertSettings:FlowMailer:OAuthEndpoint"] ?? throw new ArgumentNullException("OAuthEndpoint");
                var clientId = _configuration["AlertSettings:FlowMailer:ClientId"] ?? throw new ArgumentNullException("ClientId");
                var clientSecret = _configuration["AlertSettings:FlowMailer:ClientSecret"] ?? throw new ArgumentNullException("ClientSecret");
                var accountId = _configuration["AlertSettings:FlowMailer:AccountId"] ?? throw new ArgumentNullException("AccountId");
                return new FlowMailerAlertSender(apiEndpoint, oauthEndpoint, clientId, clientSecret, accountId);

            case AlertChannelType.Teams:
                var tenantId = _configuration["AlertSettings:Teams:TenantId"] ?? throw new ArgumentNullException("TenantId");
                var clientIdTeams = _configuration["AlertSettings:Teams:ClientId"] ?? throw new ArgumentNullException("ClientId");
                var clientSecretTeams = _configuration["AlertSettings:Teams:ClientSecret"] ?? throw new ArgumentNullException("ClientSecret");
                return new TeamsUserAlertSender(tenantId, clientIdTeams, clientSecretTeams);

            case AlertChannelType.Both:
                var senders = new List<IAlertSender>
                {
                    CreateAlertSender(AlertChannelType.Email),
                    CreateAlertSender(AlertChannelType.Teams)
                };
                return new MultiAlertSender(senders);

            // SMTP integratie
            case AlertChannelType.Smtp:
                var smtpHost = _configuration["AlertSettings:Smtp:Host"] ?? throw new ArgumentNullException("SmtpHost");
                var smtpPort = int.Parse(_configuration["AlertSettings:Smtp:Port"] ?? "587");
                var smtpUser = _configuration["AlertSettings:Smtp:User"] ?? throw new ArgumentNullException("SmtpUser");
                var smtpPass = _configuration["AlertSettings:Smtp:Pass"] ?? throw new ArgumentNullException("SmtpPass");
                var fromEmail = _configuration["AlertSettings:Smtp:From"] ?? throw new ArgumentNullException("SmtpFrom");
                return new SmtpAlertSender(smtpHost, smtpPort, smtpUser, smtpPass, fromEmail);

            default:
                throw new NotImplementedException($"Alert type {alertType} not implemented.");
        }
    }

    public static AlertChannelType ParseAlertChannelType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return AlertChannelType.Email;
        return value.Trim().ToLowerInvariant() switch
        {
            "email" => AlertChannelType.Email,
            "teams" => AlertChannelType.Teams,
            "both" => AlertChannelType.Both,
            "smtp" => AlertChannelType.Smtp,
            _ => throw new ArgumentException($"Invalid AlertType '{value}'. Allowed values: Email, Teams, Smtp, Both.")
        };
    }
}
