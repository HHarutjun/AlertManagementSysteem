using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

/// <summary>
/// Integration tests voor AlertManager.
/// </summary>
public class AlertManagerIntegrationTests
{
    /// <summary>
    /// AIT-01: Test dat een alert binnen 10 seconden wordt verstuurd bij een fatale fout (QAS-02).
    /// </summary>
    [Fact(DisplayName = "AIT-01: Verstuurt alert binnen 10 seconden bij Fatal fout (integratie)")]
    public async Task AlertSentWithin10Seconds_OnFatalError_Integration()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Endpoint: vdl-catalogus-neu | Severity: 3";
        var logs = new List<string> { log };

        var logProvider = new InMemoryLogProvider(logs);
        var sentAlertsStorage = new InMemorySentAlertsStorage();
        var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { new CorrelateByComponentStrategy() });
        var recipient = new Recipient("Test User", new List<string> { "test@example.com" }, new List<string> { "vdl-catalogus-neu" }, "Development\\Sheldon");
        var recipientsList = new List<Recipient> { recipient };
        var recipientStorage = new InMemoryRecipientStorage(recipientsList);
        var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(recipientsList), recipientStorage);
        var alertSender = new TestAlertSender();
        var taskCreator = new TestTaskCreator();

        var alertManager = new AlertManager(
            new LogProcessor(logProvider),
            new AlertCreator(),
            alertSender,
            sentAlertsStorage,
            alertStrategyManager,
            recipientResolver,
            taskCreator
        );

        // Act
        var start = DateTime.UtcNow;
        await alertManager.ProcessAlertsAsync();
        var duration = DateTime.UtcNow - start;

        // Assert
        Assert.Single(alertSender.SentAlerts);
        Assert.Equal("test@example.com", alertSender.SentAlerts[0].RecipientEmail);
        Assert.True(duration.TotalSeconds <= 10, "Alert was not sent within 10 seconds.");
    }

    /// <summary>
    /// AIT-02: Test versturen van een alert via Microsoft Teams (QAS-09).
    /// </summary>
    [Fact(DisplayName = "AIT-02: Verstuurt alert via Teams naar juiste ontvanger (integratie)")]
    public async Task AlertSentViaTeams_ToCorrectRecipient_Integration()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Endpoint: teams-comp | Severity: 3";
        var logs = new List<string> { log };

        var logProvider = new InMemoryLogProvider(logs);
        var sentAlertsStorage = new InMemorySentAlertsStorage();
        var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { new CorrelateByComponentStrategy() });
        var recipient = new Recipient("Teams User", new List<string> { "teamsuser@example.com" }, new List<string> { "teams-comp" }, "TeamsBoard");
        var recipientsList = new List<Recipient> { recipient };
        var recipientStorage = new InMemoryRecipientStorage(recipientsList);
        var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(recipientsList), recipientStorage);
        var alertSender = new TestTeamsAlertSender();
        var taskCreator = new TestTaskCreator();

        var alertManager = new AlertManager(
            new LogProcessor(logProvider),
            new AlertCreator(),
            alertSender,
            sentAlertsStorage,
            alertStrategyManager,
            recipientResolver,
            taskCreator
        );

        // Act
        await alertManager.ProcessAlertsAsync();

        // Assert
        Assert.Single(((TestTeamsAlertSender)alertSender).SentTeamsAlerts);
        Assert.Equal("teamsuser@example.com", ((TestTeamsAlertSender)alertSender).SentTeamsAlerts[0].RecipientEmail);
    }

    /// <summary>
    /// AIT-03: Test ophalen en verwerken van logs na wijziging van configuratie (QAS-11).
    /// </summary>
    [Fact(DisplayName = "AIT-03: Haalt en verwerkt logs uit gewijzigde omgeving (integratie)")]
    public async Task FetchAndProcessLogs_AfterConfigChange_Integration()
    {
        // Arrange
        // Simuleer eerst 'development' omgeving
        var devLogs = new List<string> { "Timestamp: 2024-06-01T12:00:00Z | Endpoint: dev-comp | Severity: 3" };
        var prodLogs = new List<string> { "Timestamp: 2024-06-01T13:00:00Z | Endpoint: prod-comp | Severity: 3" };

        var logProvider = new SwitchableLogProvider(devLogs);
        var sentAlertsStorage = new InMemorySentAlertsStorage();
        var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { new CorrelateByComponentStrategy() });

        var devRecipient = new Recipient("DevUser", new List<string> { "dev@example.com" }, new List<string> { "dev-comp" }, "DevBoard");
        var prodRecipient = new Recipient("ProdUser", new List<string> { "prod@example.com" }, new List<string> { "prod-comp" }, "ProdBoard");
        var recipientsList = new List<Recipient> { devRecipient, prodRecipient };
        var recipientStorage = new InMemoryRecipientStorage(recipientsList);
        var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(recipientsList), recipientStorage);
        var alertSender = new TestAlertSender();
        var taskCreator = new TestTaskCreator();

        var alertManager = new AlertManager(
            new LogProcessor(logProvider),
            new AlertCreator(),
            alertSender,
            sentAlertsStorage,
            alertStrategyManager,
            recipientResolver,
            taskCreator
        );

        // Act & Assert: Eerst development
        await alertManager.ProcessAlertsAsync();
        Assert.Single(alertSender.SentAlerts);
        Assert.Equal("dev@example.com", alertSender.SentAlerts[0].RecipientEmail);

        // Simuleer wijziging naar production
        logProvider.SetLogs(prodLogs);
        alertSender.SentAlerts.Clear();

        await alertManager.ProcessAlertsAsync();
        Assert.Single(alertSender.SentAlerts);
        Assert.Equal("prod@example.com", alertSender.SentAlerts[0].RecipientEmail);
    }

    /// <summary>
    /// AIT-04: Test dat alle kritieke fouten tijdens piekbelasting worden gemeld (QAS-13).
    /// </summary>
    [Fact(DisplayName = "AIT-04: Alle kritieke fouten worden gemeld bij piekbelasting (integratie)")]
    public async Task AllCriticalErrorsReported_UnderPeakLoad_Integration()
    {
        // Arrange
        var logs = new List<string>();
        var recipients = new Dictionary<string, Recipient>();
        for (int i = 0; i < 50; i++)
        {
            var component = $"comp{i}";
            logs.Add($"Timestamp: 2024-06-01T12:00:00Z | Endpoint: {component} | Severity: 3");
            recipients.Add(component, new Recipient($"User{i}", new List<string> { $"user{i}@example.com" }, new List<string> { component }, $"Board{i}"));
        }

        var recipientsList = recipients.Values.ToList();
        var recipientStorage = new InMemoryRecipientStorage(recipientsList);
        var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(recipientsList), recipientStorage);
        var logProvider = new InMemoryLogProvider(logs);
        var sentAlertsStorage = new InMemorySentAlertsStorage();
        var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { new CorrelateByComponentStrategy() });
        var alertSender = new TestAlertSender();
        var taskCreator = new TestTaskCreator();

        var alertManager = new AlertManager(
            new LogProcessor(logProvider),
            new AlertCreator(),
            alertSender,
            sentAlertsStorage,
            alertStrategyManager,
            recipientResolver,
            taskCreator
        );

        // Act
        await alertManager.ProcessAlertsAsync();

        // Assert
        Assert.Equal(50, alertSender.SentAlerts.Count);
        for (int i = 0; i < 50; i++)
        {
            Assert.Contains(alertSender.SentAlerts, a => a.RecipientEmail == $"user{i}@example.com");
        }
    }

    /// <summary>
    /// AIT-05: Test ophalen van logs en doorgeven aan AlertManager.
    /// </summary>
    [Fact(DisplayName = "AIT-05: Opgehaalde logs worden correct verwerkt door AlertManager (integratie)")]
    public async Task LogsFetchedAndProcessed_ByAlertManager_Integration()
    {
        // Arrange
        // Gebruik de InMemoryLogProvider als stand-in voor een echte provider
        var logs = new List<string>
        {
            "Timestamp: 2024-06-01T12:00:00Z | Endpoint: azure-comp | Severity: 3",
            "Timestamp: 2024-06-01T12:01:00Z | Endpoint: aws-comp | Severity: 3"
        };
        var logProvider = new InMemoryLogProvider(logs);
        var sentAlertsStorage = new InMemorySentAlertsStorage();
        var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { new CorrelateByComponentStrategy() });

        var recipients = new Dictionary<string, Recipient>
        {
            { "azure-comp", new Recipient("AzureUser", new List<string> { "azure@example.com" }, new List<string> { "azure-comp" }, "AzureBoard") },
            { "aws-comp", new Recipient("AWSUser", new List<string> { "aws@example.com" }, new List<string> { "aws-comp" }, "AWSBoard") }
        };
        var recipientsList = recipients.Values.ToList();
        var recipientStorage = new InMemoryRecipientStorage(recipientsList);
        var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(recipientsList), recipientStorage);
        var alertSender = new TestAlertSender();
        var taskCreator = new TestTaskCreator();

        var alertManager = new AlertManager(
            new LogProcessor(logProvider),
            new AlertCreator(),
            alertSender,
            sentAlertsStorage,
            alertStrategyManager,
            recipientResolver,
            taskCreator
        );

        // Act
        await alertManager.ProcessAlertsAsync();

        // Assert
        Assert.Equal(2, alertSender.SentAlerts.Count);
        Assert.Contains(alertSender.SentAlerts, a => a.RecipientEmail == "azure@example.com");
        Assert.Contains(alertSender.SentAlerts, a => a.RecipientEmail == "aws@example.com");
    }

    /// <summary>
    /// AIT-06: Test samenwerking AlertManager en TaskCreator bij Fatal.
    /// </summary>
    [Fact(DisplayName = "AIT-06: Werkitem wordt aangemaakt bij Fatal-status (integratie)")]
    public async Task WorkItemCreated_ByAlertManagerAndTaskCreator_OnFatal()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Endpoint: fatal-comp | Severity: 3";
        var logs = new List<string> { log };

        var logProvider = new InMemoryLogProvider(logs);
        var sentAlertsStorage = new InMemorySentAlertsStorage();
        var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { new CorrelateByComponentStrategy() });
        var recipient = new Recipient("FatalUser", new List<string> { "fatal@example.com" }, new List<string> { "fatal-comp" }, "FatalBoard");
        var recipientsList = new List<Recipient> { recipient };
        var recipientStorage = new InMemoryRecipientStorage(recipientsList);
        var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(recipientsList), recipientStorage);
        var alertSender = new TestAlertSender();
        var taskCreator = new TestTaskCreatorWithTracking();

        var alertManager = new AlertManager(
            new LogProcessor(logProvider),
            new AlertCreator(),
            alertSender,
            sentAlertsStorage,
            alertStrategyManager,
            recipientResolver,
            taskCreator
        );

        // Act
        await alertManager.ProcessAlertsAsync();

        // Assert
        Assert.Single(((TestTaskCreatorWithTracking)taskCreator).CreatedWorkItems);
        Assert.Equal("fatal-comp - Timestamp: 2024-06-01T12:00:00Z | Endpoint: fatal-comp | Severity: 3", ((TestTaskCreatorWithTracking)taskCreator).CreatedWorkItems[0].Title);
        Assert.Equal("FatalBoard", ((TestTaskCreatorWithTracking)taskCreator).CreatedWorkItems[0].Board);
    }

    /// <summary>
    /// AIT-07: Test samenwerking AlertManager en AlertSender bij Down.
    /// </summary>
    [Fact(DisplayName = "AIT-07: Alert wordt daadwerkelijk verstuurd via provider bij Down-status (integratie)")]
    public async Task AlertSent_ByAlertManagerAndAlertSender_OnDown()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Endpoint: down-comp | Severity: 3";
        var logs = new List<string> { log };

        var logProvider = new InMemoryLogProvider(logs);
        var sentAlertsStorage = new InMemorySentAlertsStorage();
        var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { new CorrelateByComponentStrategy() });
        var recipient = new Recipient("DownUser", new List<string> { "down@example.com" }, new List<string> { "down-comp" }, "DownBoard");
        var recipientsList = new List<Recipient> { recipient };
        var recipientStorage = new InMemoryRecipientStorage(recipientsList);
        var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(recipientsList), recipientStorage);
        var alertSender = new TestAlertSender();
        var taskCreator = new TestTaskCreator();

        var alertManager = new AlertManager(
            new LogProcessor(logProvider),
            new AlertCreator(),
            alertSender,
            sentAlertsStorage,
            alertStrategyManager,
            recipientResolver,
            taskCreator
        );

        // Act
        await alertManager.ProcessAlertsAsync();

        // Assert
        Assert.Single(alertSender.SentAlerts);
        Assert.Equal("down@example.com", alertSender.SentAlerts[0].RecipientEmail);
        Assert.Equal("down-comp", alertSender.SentAlerts[0].Component);
    }

    /// <summary>
    /// AIT-08: Test opslag en ophalen van sent alerts.
    /// </summary>
    [Fact(DisplayName = "AIT-08: SentAlertsStorage voorkomt dubbele alerts (integratie)")]
    public async Task SentAlertsStorage_PreventsDuplicateAlerts_Integration()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Component: storage-comp | Severity: 3";
        var logs = new List<string> { log, log };

        var sentAlertsStorage = new InMemorySentAlertsStorage();

        var logProvider = new InMemoryLogProvider(logs);
        var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { new CorrelateBySeverityStrategy() });
        var recipient = new Recipient("StorageUser", new List<string> { "storage@example.com" }, new List<string> { "storage-comp" }, "StorageBoard");
        var recipientsList = new List<Recipient> { recipient };
        var recipientStorage = new InMemoryRecipientStorage(recipientsList);
        var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(recipientsList), recipientStorage);
        var alertSender = new TestAlertSender();
        var taskCreator = new TestTaskCreator();

        var alertManager = new AlertManager(
            new LogProcessor(logProvider),
            new AlertCreator(),
            alertSender,
            sentAlertsStorage,
            alertStrategyManager,
            recipientResolver,
            taskCreator
        );

        // Act
        await alertManager.ProcessAlertsAsync();

        // Assert: only one alert should be sent for duplicate logs in the same run
        Assert.Single(alertSender.SentAlerts);
    }

    /// <summary>
    /// AIT-09: Test log grouping strategie werkt in keten.
    /// </summary>
    [Fact(DisplayName = "AIT-09: Log grouping strategy werkt in keten (integratie)")]
    public async Task LogGroupingStrategy_AppliedInChain_Integration()
    {
        // Arrange
        var now = DateTime.UtcNow.AddMinutes(-1);
        var log1 = $"Timestamp: {now:yyyy-MM-ddTHH:mm:ssZ} | Endpoint: group-comp | Severity: 3";
        var log2 = $"Timestamp: {now.AddMinutes(-5):yyyy-MM-ddTHH:mm:ssZ} | Endpoint: group-comp | Severity: 3";
        var logs = new List<string> { log1, log2 };

        var groupingStrategy = new CorrelateBySeverityStrategy();
        var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { groupingStrategy });

        var logProvider = new InMemoryLogProvider(logs);
        var sentAlertsStorage = new InMemorySentAlertsStorage();
        var recipient = new Recipient("GroupUser", new List<string> { "test@example.com" }, new List<string> { "group-comp" }, "GroupBoard");
        var recipientsList = new List<Recipient> { recipient };
        var recipientStorage = new InMemoryRecipientStorage(recipientsList);
        var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(recipientsList), recipientStorage);
        var alertSender = new TestAlertSender();
        var taskCreator = new TestTaskCreator();

        var alertManager = new AlertManager(
            new LogProcessor(logProvider),
            new AlertCreator(),
            alertSender,
            sentAlertsStorage,
            alertStrategyManager,
            recipientResolver,
            taskCreator
        );

        // Act
        await alertManager.ProcessAlertsAsync();

        // Assert: beide logs worden gegroepeerd tot één alert
        Assert.Single(alertSender.SentAlerts);
        Assert.Contains("test@example.com", alertSender.SentAlerts[0].RecipientEmail);
        Assert.Contains("group-comp", alertSender.SentAlerts[0].Message);
    }
}