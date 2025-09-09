using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using VanDijk.AlertManagement.Core.Interfaces;

/// <summary>
/// The main entry point for the AlertManagementSystem application.
/// </summary>
public class Program
{
    /// <summary>
    /// The main entry point for the application. Starts the web server if '--web' is specified, otherwise runs alert processing in console mode.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        bool runWeb = args.Any(arg => string.Equals(arg, "--web", StringComparison.OrdinalIgnoreCase));

        if (runWeb)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Load configuration from appsettings.json
            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddUserSecrets<Program>()
                .Build();

            // Set up dependency injection
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
            builder.Services.AddSingleton<AlertSenderFactory>();
            builder.Services.AddSingleton<ILogProcessor, LogProcessor>();
            builder.Services.AddSingleton<IAlertCreator, AlertCreator>();
            builder.Services.AddSingleton<IAlertSender>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var factory = provider.GetRequiredService<AlertSenderFactory>();
                var alertTypeString = config["AlertSettings:AlertType"] ?? "Email";
                var alertType = AlertSenderFactory.ParseAlertChannelType(alertTypeString);
                return factory.CreateAlertSender(alertType);
            });

            // Configure storage for sent alerts to avoid duplicate notifications
            var sentAlertsFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..",
                "Infrastructure",
                "Storage",
                "sent_alerts.json");
            sentAlertsFilePath = Path.GetFullPath(sentAlertsFilePath); // Resolve the full path
            Console.WriteLine($"[Debug] Using sent_alerts.json at: {sentAlertsFilePath}");

            // Register SentAlertsStorage for DI (zodat ISentAlertsStorage kan worden resolved)
            builder.Services.AddSingleton<SentAlertsStorage>(provider =>
                new SentAlertsStorage(sentAlertsFilePath));
            builder.Services.AddSingleton<ISentAlertsStorage>(provider =>
                provider.GetRequiredService<SentAlertsStorage>());

            // Register ILogProvider using a factory to resolve dependencies from configuration
            builder.Services.AddSingleton<ILogProvider>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var logProviderTypeString = config["LogProvider"]
                    ?? throw new ArgumentNullException("LogProvider", "Log provider is not configured.");
                if (!Enum.TryParse(logProviderTypeString, out LogProviderType logProviderType))
                {
                    throw new ArgumentException($"Invalid log provider type: {logProviderTypeString}");
                }

                return LogProviderFactory.CreateLogProvider(logProviderType, config);
            });

            // Initialize AlertStrategyManager with the default strategy
            // This determines how logs are grouped into alerts
            var defaultStrategyType = Enum.Parse<GroupingStrategyType>(
                builder.Configuration["AlertSettings:GroupingStrategy"] ?? "Correlation");
            var defaultStrategy = AlertGroupingStrategyFactory.CreateStrategy(defaultStrategyType);
            builder.Services.AddSingleton(new AlertStrategyManager(new List<IAlertGroupingStrategy> { defaultStrategy }));

            // Leeg het bestand bij elke run
            if (File.Exists(sentAlertsFilePath))
            {
                File.WriteAllText(sentAlertsFilePath, string.Empty);
                Console.WriteLine("[Debug] sent_alerts.json is leeggemaakt bij start van de applicatie.");
            }

            // Voeg deze DI-registratie toe voor RecipientStorage:
            var recipientsFilePath = Path.Combine(
                builder.Environment.ContentRootPath,
                "..",
                "Infrastructure",
                "Storage",
                "recipients.json");
            recipientsFilePath = Path.GetFullPath(recipientsFilePath);
            builder.Services.AddSingleton(new RecipientStorage(recipientsFilePath));

            // Configure recipient resolver to map components to recipients
            builder.Services.AddSingleton<IRecipientResolver>(provider =>
            {
                var recipientStorage = provider.GetRequiredService<RecipientStorage>();
                var recipients = recipientStorage.LoadRecipients();
                var strategy = new RecipientRoutingStrategy(recipients);
                return new RecipientResolver(strategy, recipientStorage);
            });

            // Register ISprintService
            builder.Services.AddSingleton<ISprintService>(provider =>
            {
                var organizationUrl = builder.Configuration["AzureDevOps:OrganizationUrl"]
                    ?? throw new ArgumentNullException("OrganizationUrl", "Azure DevOps organization URL is not configured.");
                var projectName = builder.Configuration["AzureDevOps:ProjectName"]
                    ?? throw new ArgumentNullException("ProjectName", "Azure DevOps project name is not configured.");
                var personalAccessToken = builder.Configuration["AzureDevOps:PersonalAccessToken"]
                    ?? throw new ArgumentNullException("PersonalAccessToken", "Azure DevOps personal access token is not configured.");

                return new SprintService(organizationUrl, projectName, personalAccessToken);
            });

            // Register ITaskCreator and inject ISprintService
            builder.Services.AddSingleton<ITaskCreator>(provider =>
            {
                var organizationUrl = builder.Configuration["AzureDevOps:OrganizationUrl"]
                    ?? throw new ArgumentNullException("OrganizationUrl", "Azure DevOps organization URL is not configured.");
                var projectName = builder.Configuration["AzureDevOps:ProjectName"]
                    ?? throw new ArgumentNullException("ProjectName", "Azure DevOps project name is not configured.");
                var personalAccessToken = builder.Configuration["AzureDevOps:PersonalAccessToken"]
                    ?? throw new ArgumentNullException("PersonalAccessToken", "Azure DevOps personal access token is not configured.");

                var sprintService = provider.GetRequiredService<ISprintService>();
                return new TaskCreator(organizationUrl, projectName, personalAccessToken, sprintService);
            });

            // Voeg deze regel toe vóór builder.Build():
            builder.Services.AddControllers();
            builder.Services.AddRazorPages(); // <-- Razor Pages toevoegen

            var app = builder.Build();

            // Serve static files from Templates folder
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(builder.Environment.ContentRootPath, "Templates")),
                RequestPath = "/Templates",
            });

            app.MapControllers();
            app.MapRazorPages(); // <-- Razor Pages endpoints toevoegen

            app.Run();
        }
        else
        {
            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddUserSecrets<Program>()
                .Build();

            // Set up dependency injection
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<AlertSenderFactory>();
            services.AddSingleton<ILogProcessor, LogProcessor>();
            services.AddSingleton<IAlertCreator, AlertCreator>();
            services.AddSingleton<IAlertSender>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var factory = provider.GetRequiredService<AlertSenderFactory>();
                var alertTypeString = config["AlertSettings:AlertType"] ?? "Email";
                var alertType = AlertSenderFactory.ParseAlertChannelType(alertTypeString);
                return factory.CreateAlertSender(alertType);
            });

            // Register ISentAlertsStorage for DI
            services.AddSingleton<ISentAlertsStorage>(provider =>
                provider.GetRequiredService<SentAlertsStorage>());

            // Register ILogProvider using a factory to resolve dependencies from configuration
            services.AddSingleton<ILogProvider>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var logProviderTypeString = config["LogProvider"]
                    ?? throw new ArgumentNullException("LogProvider", "Log provider is not configured.");
                if (!Enum.TryParse(logProviderTypeString, out LogProviderType logProviderType))
                {
                    throw new ArgumentException($"Invalid log provider type: {logProviderTypeString}");
                }

                return LogProviderFactory.CreateLogProvider(logProviderType, config);
            });

            // Voeg deze DI-registratie toe voor RecipientStorage:
            var recipientsFilePath2 = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..",
                "Infrastructure",
                "Storage",
                "recipients.json");
            recipientsFilePath2 = Path.GetFullPath(recipientsFilePath2);
            services.AddSingleton(new RecipientStorage(recipientsFilePath2));

            // Configure recipient resolver to map components to recipients
            services.AddSingleton<IRecipientResolver>(provider =>
            {
                var recipientStorage = provider.GetRequiredService<RecipientStorage>();
                var recipients = recipientStorage.LoadRecipients();
                var strategy = new RecipientRoutingStrategy(recipients);
                return new RecipientResolver(strategy, recipientStorage);
            });

            // Register SentAlertsStorage for DI (zodat ISentAlertsStorage kan worden resolved)
            var sentAlertsFilePath2 = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..",
                "Infrastructure",
                "Storage",
                "sent_alerts.json");
            var sentAlertsFullPath2 = Path.GetFullPath(sentAlertsFilePath2);
            services.AddSingleton<SentAlertsStorage>(provider =>
                new SentAlertsStorage(sentAlertsFullPath2));
            services.AddSingleton<ISentAlertsStorage>(provider =>
                provider.GetRequiredService<SentAlertsStorage>());

            // Register ITaskCreator and ISprintService for DI
            services.AddSingleton<ISprintService>(provider =>
            {
                var organizationUrl = configuration["AzureDevOps:OrganizationUrl"]
                    ?? throw new ArgumentNullException("OrganizationUrl", "Azure DevOps organization URL is not configured.");
                var projectName = configuration["AzureDevOps:ProjectName"]
                    ?? throw new ArgumentNullException("ProjectName", "Azure DevOps project name is not configured.");
                var personalAccessToken = configuration["AzureDevOps:PersonalAccessToken"]
                    ?? throw new ArgumentNullException("PersonalAccessToken", "Azure DevOps personal access token is not configured.");

                return new SprintService(organizationUrl, projectName, personalAccessToken);
            });

            services.AddSingleton<ITaskCreator>(provider =>
            {
                var organizationUrl = configuration["AzureDevOps:OrganizationUrl"]
                    ?? throw new ArgumentNullException("OrganizationUrl", "Azure DevOps organization URL is not configured.");
                var projectName = configuration["AzureDevOps:ProjectName"]
                    ?? throw new ArgumentNullException("ProjectName", "Azure DevOps project name is not configured.");
                var personalAccessToken = configuration["AzureDevOps:PersonalAccessToken"]
                    ?? throw new ArgumentNullException("PersonalAccessToken", "Azure DevOps personal access token is not configured.");

                var sprintService = provider.GetRequiredService<ISprintService>();
                return new TaskCreator(organizationUrl, projectName, personalAccessToken, sprintService);
            });

            // Register AlertStrategyManager for DI (zodat deze resolved kan worden)
            var defaultStrategyType2 = Enum.Parse<GroupingStrategyType>(
                configuration["AlertSettings:GroupingStrategy"] ?? "Correlation");
            var defaultStrategy2 = AlertGroupingStrategyFactory.CreateStrategy(defaultStrategyType2);
            services.AddSingleton(new AlertStrategyManager(new List<IAlertGroupingStrategy> { defaultStrategy2 }));

            // Register AlertManager for DI
            services.AddSingleton<AlertManager>(provider =>
            {
                var logProvider = provider.GetRequiredService<ILogProvider>();
                var logProcessor = provider.GetRequiredService<ILogProcessor>();
                var alertCreator = provider.GetRequiredService<IAlertCreator>();
                var alertSender = provider.GetRequiredService<IAlertSender>();
                var sentAlertsStorage = provider.GetRequiredService<SentAlertsStorage>();
                var alertStrategyManager = provider.GetRequiredService<AlertStrategyManager>();
                var recipientResolver = provider.GetRequiredService<IRecipientResolver>();
                var taskCreator = provider.GetRequiredService<ITaskCreator>();
                return new AlertManager(
                    logProcessor,
                    alertCreator,
                    alertSender,
                    sentAlertsStorage,
                    alertStrategyManager,
                    recipientResolver,
                    taskCreator);
            });

            // Build the service provider once
#pragma warning disable ASP0000 // Suppress warning about BuildServiceProvider in console app context
            using var serviceProvider = services.BuildServiceProvider();
#pragma warning restore ASP0000

            // Resolve AlertManager and start processing alerts
            var alertManager = serviceProvider.GetRequiredService<AlertManager>();
            await alertManager.ProcessAlertsAsync();
        }
    }
}