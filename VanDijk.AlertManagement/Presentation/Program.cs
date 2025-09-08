using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Storage;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using System.Threading.Tasks;
using System.Linq;

class Program
{
    static async Task Main(string[] args)
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
                "sent_alerts.json"
            );
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
                builder.Configuration["AlertSettings:GroupingStrategy"] ?? "Correlation"
            );
            var defaultStrategy = AlertGroupingStrategyFactory.CreateStrategy(defaultStrategyType);
            builder.Services.AddSingleton(new AlertStrategyManager(new List<IAlertGroupingStrategy> { defaultStrategy }));

            // Leeg het bestand bij elke run
            if (File.Exists(sentAlertsFilePath))
            {
                File.WriteAllText(sentAlertsFilePath, "");
                Console.WriteLine("[Debug] sent_alerts.json is leeggemaakt bij start van de applicatie.");
            }

            // Voeg deze DI-registratie toe voor RecipientStorage:
            var recipientsFilePath = Path.Combine(
                builder.Environment.ContentRootPath,
                "..",
                "Infrastructure",
                "Storage",
                "recipients.json"
            );
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
                RequestPath = "/Templates"
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
                "recipients.json"
            );
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
                "sent_alerts.json"
            );
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
                configuration["AlertSettings:GroupingStrategy"] ?? "Correlation"
            );
            var defaultStrategy2 = AlertGroupingStrategyFactory.CreateStrategy(defaultStrategyType2);
            services.AddSingleton(new AlertStrategyManager(new List<IAlertGroupingStrategy> { defaultStrategy2 }));

            // Build the service provider to resolve dependencies
            var serviceProvider = services.BuildServiceProvider();

            // Resolve dependencies for alert processing
            var alertSenderFactory = serviceProvider.GetRequiredService<AlertSenderFactory>();
            var sentAlertsStorage = serviceProvider.GetRequiredService<SentAlertsStorage>();
            var taskCreator = serviceProvider.GetRequiredService<ITaskCreator>();

            // Configure the log provider (e.g., Azure or AWS)
            var logProviderTypeString = configuration["LogProvider"] 
                ?? throw new ArgumentNullException("LogProvider", "Log provider is not configured.");
            if (!Enum.TryParse(logProviderTypeString, out LogProviderType logProviderType))
            {
                throw new ArgumentException($"Invalid log provider type: {logProviderTypeString}");
            }
            var logProvider = LogProviderFactory.CreateLogProvider(logProviderType, configuration);

            // Configure the alert sender (e.g., FlowMailer for email notifications)
            var alertTypeString = configuration["AlertSettings:AlertType"] 
                ?? throw new ArgumentNullException("AlertType", "Alert type is not configured.");
            var alertType = AlertSenderFactory.ParseAlertChannelType(alertTypeString);
            var alertSender = alertSenderFactory.CreateAlertSender(alertType);

            // Initialize the AlertManager, which orchestrates the entire alerting process
            var recipientResolver = serviceProvider.GetRequiredService<IRecipientResolver>();
            recipientResolver.ResolveRecipient(new Alert { Component = "func-tln-authorization-public-weu-tst" });
            var alertManager = new AlertManager(
                new LogProcessor(logProvider),
                new AlertCreator(),
                alertSender,
                sentAlertsStorage,
                serviceProvider.GetRequiredService<AlertStrategyManager>(),
                recipientResolver,
                taskCreator
            );

            // Start processing alerts
            await alertManager.ProcessAlertsAsync();
        }
    }
}