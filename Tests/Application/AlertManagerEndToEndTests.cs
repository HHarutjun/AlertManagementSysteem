using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Xunit;

/// <summary>
/// End-to-end tests voor AlertManager.
/// </summary>
public class AlertManagerEndToEndTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper output;
    public AlertManagerEndToEndTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        this.output = output;
    }

    /// <summary>
    /// AEET-01: Test end-to-end: van log tot e-mail alert.
    /// </summary>
    [Fact(DisplayName = "AEET-01: End-to-end van log tot e-mail alert")]
    public async Task EndToEnd_LogToEmailAlert()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Component: func-vdl-lmlnext-proxy-weu-prd | Severity: 3 | ProblemId: System.Web.HttpException at System.Web.Mvc.DefaultControllerFactory.GetControllerInstance | ExceptionType: System.Web.HttpException";
        var logs = new List<string> { log };

        var logProvider = new InMemoryLogProvider(logs);
        var sentAlertsStorage = new InMemorySentAlertsStorage();
        var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { new CorrelateBySeverityStrategy() });
        var recipient = new Recipient(
            "E2E User",
            new List<string> { "e2euser@example.com" },
            new List<string> { "func-vdl-lmlnext-proxy-weu-prd" },
            "E2EBoard",
            GroupingStrategyType.Severity
        );
        var routing = new List<Recipient> { recipient };
        var inMemoryRecipientStorage = new InMemoryRecipientStorage(routing);
        var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(routing), inMemoryRecipientStorage);
        var alertSender = new TestEmailAlertSender();
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

        // Make sure routing strategy is updated with recipients from storage
        recipientResolver.RefreshRecipients();

        // Act
        await alertManager.ProcessAlertsAsync();
        // Assert

        Assert.Single(alertSender.SentEmails);
        var emailResult = alertSender.SentEmails[0];
        Assert.Equal("e2euser@example.com", emailResult.RecipientEmail);
        Assert.Contains("func-vdl-lmlnext-proxy-weu-prd", emailResult.Message);
        Assert.Contains("2024-06-01T12:00:00Z", emailResult.Message);
        Assert.Contains("Severity: 3", emailResult.Message);
    }

    /// <summary>
    /// AEET-02: Test end-to-end: van log tot Azure DevOps werkitem.
    /// </summary>
    [Fact(DisplayName = "AEET-02: End-to-end van log tot Azure DevOps werkitem")]
    public async Task EndToEnd_LogToAzureDevOpsWorkItem()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Component: e2e-devops | Severity: 3";
        var logs = new List<string> { log };

        var logProvider = new InMemoryLogProvider(logs);
        var sentAlertsStorage = new InMemorySentAlertsStorage();
        var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { new CorrelateBySeverityStrategy() });
        var recipient = new Recipient(
            "DevOps User",
            new List<string> { "devops@example.com" },
            new List<string> { "e2e-devops" },
            "DevOpsBoard",
            GroupingStrategyType.Severity
        );
        var routing = new List<Recipient> { recipient };
        var inMemoryRecipientStorage = new InMemoryRecipientStorage(routing);
        var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(routing), inMemoryRecipientStorage);
        var alertSender = new TestEmailAlertSender();
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

        recipientResolver.RefreshRecipients();

        // Act
        await alertManager.ProcessAlertsAsync();

        // Assert
        Assert.Single(alertSender.SentEmails);
        Assert.Single(taskCreator.CreatedWorkItems);
        Assert.Equal("devops@example.com", alertSender.SentEmails[0].RecipientEmail);
        Assert.Equal("e2e-devops - Timestamp: 2024-06-01T12:00:00Z | Component: e2e-devops | Severity: 3", taskCreator.CreatedWorkItems[0].Title);
        Assert.Equal("DevOpsBoard", taskCreator.CreatedWorkItems[0].Board);
    }

    /// <summary>
    /// AEET-03: Test end-to-end: geen dubbele alerts of taken bij herhaalde logs.
    /// </summary>
    [Fact(DisplayName = "AEET-03: End-to-end geen dubbele alerts of taken bij herhaalde logs")]
    public async Task EndToEnd_NoDuplicateAlertsOrTasks_ForRepeatedLogs()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Component: e2e-dup | Severity: 3";
        var logs = new List<string> { log };

        var logProvider = new InMemoryLogProvider(logs);
        var sentAlertsStorage = new InMemorySentAlertsStorage();
        var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { new CorrelateBySeverityStrategy() });
        var recipient = new Recipient(
            "DupUser",
            new List<string> { "dup@example.com" },
            new List<string> { "e2e-dup" },
            "DupBoard",
            GroupingStrategyType.Severity
        );
        var routing = new List<Recipient> { recipient };
        var inMemoryRecipientStorage = new InMemoryRecipientStorage(routing);
        var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(routing), inMemoryRecipientStorage);
        var alertSender = new TestEmailAlertSender();
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

        // Make sure routing strategy is updated with recipients from storage
        recipientResolver.RefreshRecipients();

        // Act: eerste keer
        await alertManager.ProcessAlertsAsync();

        // Assert: eerste keer
        Assert.Single(alertSender.SentEmails);
        Assert.Single(taskCreator.CreatedWorkItems);

        // Act: tweede keer (zelfde log)
        // Gebruik dezelfde alertManager en sentAlertsStorage!
        await alertManager.ProcessAlertsAsync();

        //Assert: tweede keer
        Assert.Single(taskCreator.CreatedWorkItems);
    }

    /// <summary>
    /// AEET-04: Test end-to-end: verwerken van logs uit meerdere bronnen.
    /// </summary>
    [Fact(DisplayName = "AEET-04: End-to-end verwerken van logs uit Azure en AWS")]
    public async Task EndToEnd_ProcessLogsFromMultipleSources()
    {
        // Arrange
        var azureLog = "Timestamp: 2024-06-01T12:00:00Z | Endpoint: azure-e2e | Severity: 3";
        var awsLog = "Timestamp: 2024-06-01T12:01:00Z | Endpoint: aws-e2e | Severity: 3";
        var logs = new List<string> { azureLog, awsLog };

        var logProvider = new InMemoryLogProvider(logs); // Simuleer gecombineerde provider
        var sentAlertsStorage = new InMemorySentAlertsStorage();
        var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { new CorrelateByComponentStrategy() });

        var recipients = new List<Recipient>
        {
            new Recipient("AzureE2E", new List<string> { "azuree2e@example.com" }, new List<string> { "azure-e2e" }, "AzureE2EBoard", GroupingStrategyType.Component),
            new Recipient("AWSE2E", new List<string> { "awse2e@example.com" }, new List<string> { "aws-e2e" }, "AWSE2EBoard", GroupingStrategyType.Component)
        };
        var inMemoryRecipientStorage = new InMemoryRecipientStorage(recipients);
        var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(recipients), inMemoryRecipientStorage);
        var alertSender = new TestEmailAlertSender();
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

        recipientResolver.RefreshRecipients();

        // Act
        await alertManager.ProcessAlertsAsync();

        // Assert
        Assert.Equal(2, alertSender.SentEmails.Count);
        Assert.Equal(2, taskCreator.CreatedWorkItems.Count);
        Assert.Contains(alertSender.SentEmails, e => e.RecipientEmail == "azuree2e@example.com");
        Assert.Contains(alertSender.SentEmails, e => e.RecipientEmail == "awse2e@example.com");
        Assert.Contains(taskCreator.CreatedWorkItems, w => w.Board == "AzureE2EBoard");
        Assert.Contains(taskCreator.CreatedWorkItems, w => w.Board == "AWSE2EBoard");
    }

    /// <summary>
    /// AEET-05: Test end-to-end: foutafhandeling bij falende externe systemen.
    /// </summary>
    [Fact(DisplayName = "AEET-05: End-to-end foutafhandeling bij falende externe systemen")]
    public async Task EndToEnd_ErrorHandling_WhenExternalSystemsFail()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Endpoint: fail-comp | Status: Down";
        var logs = new List<string> { log };

        var logProvider = new InMemoryLogProvider(logs);
        var sentAlertsStorage = new InMemorySentAlertsStorage();
        var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { new CorrelateByComponentStrategy() });
        var recipient = new Recipient("FailUser", new List<string> { "fail@example.com" }, new List<string> { "fail-comp" }, "FailBoard");
        var routing = new List<Recipient> { recipient };
        var recipientStorage = new RecipientStorage("testRecipients.json");
        var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(routing), recipientStorage);
        var alertSender = new FailingEmailAlertSender();
        var taskCreator = new FailingTaskCreator();

        var alertManager = new AlertManager(
            new LogProcessor(logProvider),
            new AlertCreator(),
            alertSender,
            sentAlertsStorage,
            alertStrategyManager,
            recipientResolver,
            taskCreator
        );

        // Act & Assert: mag niet throwen ondanks exceptions
        var ex = await Record.ExceptionAsync(() => alertManager.ProcessAlertsAsync());
        Assert.Null(ex);
    }

    /// <summary>
    /// AEET-06: Test end-to-end: Correcte afhandeling KnownProblemsFilterStrategy.
    /// </summary>
    [Fact(DisplayName = "AEET-06: End-to-end: Correcte afhandeling KnownProblemsFilterStrategy")]
public async Task EndToEnd_KnownProblemsFilterStrategy_WorksCorrectly()
{
    // Arrange
    var component = "ComponentA";
    var problemId1 = "ProblemId1";
    var problemId2 = "ProblemId2";
    var log1 = $"Timestamp: 2024-06-01T12:00:00Z | Component: {component} | Severity: 3 | ProblemId: {problemId1}";
    var log2 = $"Timestamp: 2024-06-01T12:05:00Z | Component: {component} | Severity: 3 | ProblemId: {problemId2}";
    var log3 = $"Timestamp: 2024-06-01T12:10:00Z | Component: {component} | Severity: 3 | ProblemId: {problemId2}";

    var logs = new List<string> { log1 };

    var logProvider = new InMemoryLogProvider(logs);
    var sentAlertsStorage = new InMemorySentAlertsStorage();
    var alertSender = new TestEmailAlertSender();
    var taskCreator = new TestTaskCreatorWithDescriptionUpdate();

    var recipient = new Recipient(
        "KnownUser",
        new List<string> { "known@example.com" },
        new List<string> { component },
        "KnownBoard",
        GroupingStrategyType.KnownProblemsFilter
    );
    var recipients = new List<Recipient> { recipient };
    var inMemoryRecipientStorage = new InMemoryRecipientStorage(recipients);
    var recipientResolver = new RecipientResolver(new RecipientRoutingStrategy(recipients), inMemoryRecipientStorage);

    // Use the descriptions from the test double
    Func<string, HashSet<string>> getKnownProblemsForComponent = comp =>
    {
        var title = $"{comp} - KnownProblems";
        var known = new HashSet<string>();
        if (taskCreator.Descriptions.TryGetValue(title, out var desc))
        {
            foreach (var line in desc.Split('\n'))
            {
                if (line.Contains("ProblemId:"))
                {
                    var pid = line.Split("ProblemId:").Last().Trim();
                    if (!string.IsNullOrWhiteSpace(pid))
                        known.Add(pid);
                }
            }
        }
        return known;
    };

    var alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy>
    {
        new KnownProblemsFilterStrategy(getKnownProblemsForComponent)
    });

    var alertManager = new AlertManager(
        new LogProcessor(logProvider),
        new AlertCreator(),
        alertSender,
        sentAlertsStorage,
        alertStrategyManager,
        recipientResolver,
        taskCreator
    );

    recipientResolver.RefreshRecipients();

    // Act 1: Eerste probleem
    await alertManager.ProcessAlertsAsync();

    // Assert 1: Er is een alert verstuurd en een taak aangemaakt
    Assert.Single(alertSender.SentEmails);
    Assert.Single(taskCreator.Descriptions);
    Assert.Contains(problemId1, taskCreator.Descriptions[$"{component} - KnownProblems"]);

    // Act 2: Tweede probleem (nieuw problemId)
    logs.Clear();
    logs.Add(log2);
    await alertManager.ProcessAlertsAsync();

    // Assert 2: Er is een tweede alert verstuurd en description bevat beide problemIds
    Assert.Equal(2, alertSender.SentEmails.Count);
    var desc = taskCreator.Descriptions[$"{component} - KnownProblems"];
    Assert.Contains(problemId1, desc);
    Assert.Contains(problemId2, desc);

    // Act 3: Herhaal tweede probleemId (moet geen nieuwe alert geven)
    logs.Clear();
    logs.Add(log3);
    await alertManager.ProcessAlertsAsync();

    // Assert 3: Geen extra alert verstuurd
    Assert.Equal(2, alertSender.SentEmails.Count);
}
}
