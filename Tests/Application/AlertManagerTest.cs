using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

/// <summary>
/// Testklasse voor <see cref="AlertManager"/>.
/// </summary>
public class AlertManagerTest
{
    private readonly Xunit.Abstractions.ITestOutputHelper output;
    // Globale variabelen voor SUT en mocks
    private AlertManager sut;
    private Mock<ILogProvider> mockLogProvider;
    private Mock<IAlertSender> mockAlertSender;
    private Mock<ISentAlertsStorage> mockSentAlertsStorage;
    private Mock<IRecipientResolver> mockRecipientResolver;
    private Mock<ITaskCreator> mockTaskCreator;
    private AlertStrategyManager alertStrategyManager;

    /// <summary>
    /// Initialiseert mocks en SUT.
    /// </summary>
    public AlertManagerTest(Xunit.Abstractions.ITestOutputHelper output)
    {
        this.output = output;
        // Arrange: initialiseer mocks en sut
        mockLogProvider = new Mock<ILogProvider>();
        mockAlertSender = new Mock<IAlertSender>();
        mockSentAlertsStorage = new Mock<ISentAlertsStorage>();
        mockRecipientResolver = new Mock<IRecipientResolver>();
        mockTaskCreator = new Mock<ITaskCreator>();

        // Sent alerts storage mock
        mockSentAlertsStorage.Setup(s => s.LoadSentAlerts()).Returns(new HashSet<string>());
        // AlertStrategyManager met een dummy strategie
        alertStrategyManager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { new CorrelateBySeverityStrategy() });

        var logProcessor = new LogProcessor(mockLogProvider.Object);
        var alertCreator = new AlertCreator();

        sut = new AlertManager(
            logProcessor,
            alertCreator,
            mockAlertSender.Object,
            mockSentAlertsStorage.Object,
            alertStrategyManager,
            mockRecipientResolver.Object,
            mockTaskCreator.Object
        );
    }

    /// <summary>
    /// AUT-02: Test geen alert verzenden wanneer een component Info is.
    /// </summary>
    [Fact(DisplayName = "AUT-02: Geen alert verzenden bij SeverityLevel Info")]
    public async Task NoAlertSent_WhenSeverityLevelIsInfo()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Endpoint: vdl-catalogus-neu | Severity: 1";
        mockLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(new List<string> { log });

        var recipient = new Recipient("Test User",new List<string> { "test@example.com" }, new List<string> { "vdl-catalogus-neu" }, "Development\\Sheldon");
        mockRecipientResolver.Setup(r => r.ResolveRecipient(It.IsAny<Alert>())).Returns(recipient);

        mockTaskCreator.Setup(t => t.TaskExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        mockTaskCreator.Setup(t => t.CreateWorkItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WorkItemType>()))
            .ReturnsAsync((string?)null);

        // Act
        await sut.ProcessAlertsAsync();

        // Assert
        mockAlertSender.Verify(sender => sender.SendAlertAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Never);
    }

    /// <summary>
    /// AUT-03: Test aanmaken van Azure DevOps taak bij Fatal.
    /// </summary>
    [Fact(DisplayName = "AUT-03: Azure DevOps werkitem wordt aangemaakt bij Fatal-status")]
    public async Task WorkItemCreated_WhenFatalStatus()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Component: vdl-catalogus-neu | Severity: 3 | ExceptionType: System.Threading.Tasks.TaskCanceledException";
        mockLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(new List<string> { log });

        var recipient = new Recipient(
            "Test User",
            new List<string> { "test@example.com" },
            new List<string> { "vdl-catalogus-neu" },
            "Development\\Sheldon",
            GroupingStrategyType.Severity
        );

        output.WriteLine($"Recipient: Name={recipient.Name}, Emails=[{string.Join(",", recipient.Emails)}], ResponsibleComponents=[{string.Join(",", recipient.ResponsibleComponents)}], Board={recipient.Board}");

        var fakeRecipientResolver = new FakeRecipientResolver(new List<Recipient> { recipient });

        var capturingTaskCreator = new CapturingTaskCreator();

        sut = new AlertManager(
            new LogProcessor(mockLogProvider.Object),
            new AlertCreator(),
            mockAlertSender.Object,
            mockSentAlertsStorage.Object,
            alertStrategyManager,
            fakeRecipientResolver,
            capturingTaskCreator
        );
        // Act
        await sut.ProcessAlertsAsync();

        // Assert
        output.WriteLine("Actual created titles: " + string.Join(", ", capturingTaskCreator.CreatedTitles));
        Assert.Contains("vdl-catalogus-neu - System.Threading.Tasks.TaskCanceledException", capturingTaskCreator.CreatedTitles);
    }

    /// <summary>
    /// AUT-04: Test geen taak aanmaken bij Info/Warning.
    /// </summary>
    [Theory(DisplayName = "AUT-04: Geen werkitem bij Info of Warning status")]
    [InlineData("Up", "Info")]
    [InlineData("Warning", "Warning")]
    public async Task NoWorkItemCreated_WhenInfoOrWarningStatus(string statusText, string expectedSeverity)
    {
        // Arrange
        var log = $"Timestamp: 2024-06-01T12:00:00Z | Endpoint: vdl-catalogus-neu | Severity: {(statusText == "Up" ? "1" : statusText == "Warning" ? "2" : "3")}";

        mockLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(new List<string> { log });

        var recipient = new Recipient("Test User", new List<string> { "test@example.com" }, new List<string> { "vdl-catalogus-neu" }, "Development\\Sheldon", GroupingStrategyType.Severity );
        mockRecipientResolver.Setup(r => r.ResolveRecipient(It.IsAny<Alert>())).Returns(recipient);

        // Act
        await sut.ProcessAlertsAsync();

        // Assert
        mockTaskCreator.Verify(t => t.CreateWorkItemAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<WorkItemType>()
        ), Times.Never);
    }

    /// <summary>
    /// AUT-05: Test geen dubbele alerts bij reeds verstuurde meldingen.
    /// </summary>
    [Fact(DisplayName = "AUT-05: Geen dubbele alert bij reeds verstuurde melding")]
    public async Task NoDuplicateAlert_WhenAlreadySent()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Endpoint: vdl-catalogus-neu | Severity: 3";
        var sentKey = "Fatal|test@example.com";
        mockLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(new List<string> { log });

        var recipient = new Recipient("Test User", new List<string> { "test@example.com" }, new List<string> { "vdl-catalogus-neu" }, "Development\\Sheldon", GroupingStrategyType.Severity );
        mockRecipientResolver.Setup(r => r.ResolveRecipient(It.IsAny<Alert>())).Returns(recipient);

        // Simuleer dat deze alert al verstuurd is
        mockSentAlertsStorage.Setup(s => s.LoadSentAlerts()).Returns(new HashSet<string> { sentKey });

        // SUT opnieuw initialiseren zodat _sentAlerts gevuld is
        var logProcessor = new LogProcessor(mockLogProvider.Object);
        var alertCreator = new AlertCreator();
        sut = new AlertManager(
            logProcessor,
            alertCreator,
            mockAlertSender.Object,
            mockSentAlertsStorage.Object,
            alertStrategyManager,
            mockRecipientResolver.Object,
            mockTaskCreator.Object
        );

        // Act
        await sut.ProcessAlertsAsync();

        // Assert
        mockAlertSender.Verify(sender => sender.SendAlertAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Never);
    }

    /// <summary>
    /// AUT-06: Test correcte ontvanger bij meerdere componenten.
    /// </summary>
    [Fact(DisplayName = "AUT-06: Juiste ontvanger bij meerdere componenten")]
    public async Task CorrectRecipient_WhenMultipleComponents()
    {
        // Arrange
        var log1 = "Timestamp: 2024-06-01T12:00:00Z | Component: vdl-catalogus-neu | Severity: 3";
        var log2 = "Timestamp: 2024-06-01T12:00:00Z | Component: vdl-orders | Severity: 3";

        var recipient1 = new Recipient(
            "User1",
            new List<string> { "user1@example.com" },
            new List<string> { "vdl-catalogus-neu" },
            "Development\\Sheldon",
            GroupingStrategyType.Severity
        );
        var recipient2 = new Recipient(
            "User2",
            new List<string> { "user2@example.com" },
            new List<string> { "vdl-orders" },
            "Development\\Leonard",
            GroupingStrategyType.Severity
        );

        mockLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(new List<string> { log1, log2 });

        var fakeRecipientResolver = new FakeRecipientResolver(new List<Recipient> { recipient1, recipient2 });
        var capturingTaskCreator = new CapturingTaskCreator();

        sut = new AlertManager(
            new LogProcessor(mockLogProvider.Object),
            new AlertCreator(),
            mockAlertSender.Object,
            mockSentAlertsStorage.Object,
            alertStrategyManager,
            fakeRecipientResolver,
            capturingTaskCreator
        );

        // Act
        await sut.ProcessAlertsAsync();

        // Assert
        var expectedComponentKey1 = "Fatal";
        var expectedComponentKey2 = "Fatal";

        mockAlertSender.Verify(sender => sender.SendAlertAsync(
            It.Is<string>(msg => msg.Contains("vdl-catalogus-neu")),
            recipient1.Emails[0],
            expectedComponentKey1
        ), Times.Once);

        mockAlertSender.Verify(sender => sender.SendAlertAsync(
            It.Is<string>(msg => msg.Contains("vdl-orders")),
            recipient2.Emails[0],
            expectedComponentKey2
        ), Times.Once);
    }

    /// <summary>
    /// AUT-07: Test foutafhandeling bij ontbreken van recipient.
    /// </summary>
    [Fact(DisplayName = "AUT-07: Geen alert en foutmelding bij ontbreken van recipient")]
    public async Task NoAlertSent_WhenRecipientMissing()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Endpoint: onbekend-component | Severity: 3";
        mockLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(new List<string> { log });

        // Simuleer dat er geen recipient gevonden wordt
        mockRecipientResolver.Setup(r => r.ResolveRecipient(It.IsAny<Alert>())).Returns((Recipient)null);

        // Act
        await sut.ProcessAlertsAsync();

        // Assert
        mockAlertSender.Verify(sender => sender.SendAlertAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Never);
    }

    /// <summary>
    /// AUT-08: Test grouping strategy: logs worden correct gegroepeerd.
    /// </summary>
    [Fact(DisplayName = "AUT-08: Logs worden correct gegroepeerd per strategie")]
    public async Task LogsGroupedCorrectly_ByStrategy()
    {
        // Arrange
        var log1 = "Timestamp: 2024-06-01T12:00:00Z | Endpoint: compA | Severity: 3";
        var log2 = "Timestamp: 2024-06-01T12:02:00Z | Endpoint: compB | Severity: 3";
        mockLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(new List<string> { log1, log2 });

        var recipientA = new Recipient("UserA", new List<string> { "a@example.com" }, new List<string> { "compA" }, "BoardA", GroupingStrategyType.Severity );
        var recipientB = new Recipient("UserB", new List<string> { "b@example.com" }, new List<string> { "compB" }, "BoardB", GroupingStrategyType.Severity );

        var fakeRecipientResolver = new FakeRecipientResolver(new List<Recipient> { recipientA, recipientB });
        var capturingTaskCreator = new CapturingTaskCreator();

        sut = new AlertManager(
            new LogProcessor(mockLogProvider.Object),
            new AlertCreator(),
            mockAlertSender.Object,
            mockSentAlertsStorage.Object,
            alertStrategyManager,
            fakeRecipientResolver,
            capturingTaskCreator
        );

        mockTaskCreator.Setup(t => t.TaskExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        mockTaskCreator.Setup(t => t.CreateWorkItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WorkItemType>()))
            .ReturnsAsync((string?)null);

        // Act
        await sut.ProcessAlertsAsync();

        var expectedKeyA = "Fatal";
        var expectedKeyB = "Fatal";
        mockAlertSender.Verify(sender => sender.SendAlertAsync(
            It.Is<string>(msg => msg.Contains("compA")),
            recipientA.Emails[0],
            expectedKeyA
        ), Times.Once);

        mockAlertSender.Verify(sender => sender.SendAlertAsync(
            It.Is<string>(msg => msg.Contains("compB")),
            recipientB.Emails[0],
            expectedKeyB
        ), Times.Once);
    }

    /// <summary>
    /// AUT-09: Test dat AlertSender exceptions worden opgevangen.
    /// </summary>
    [Fact(DisplayName = "AUT-09: AlertSender exception wordt opgevangen en gelogd")]
    public async Task AlertSenderException_IsHandledAndLogged()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Endpoint: compX | Severity: 3";
        mockLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(new List<string> { log });

        var recipient = new Recipient("UserX", new List<string> { "x@example.com" }, new List<string> { "compX" }, "BoardX", GroupingStrategyType.Severity );
        mockRecipientResolver.Setup(r => r.ResolveRecipient(It.IsAny<Alert>())).Returns(recipient);

        mockTaskCreator.Setup(t => t.TaskExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        mockTaskCreator.Setup(t => t.CreateWorkItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WorkItemType>()))
            .ReturnsAsync((string?)null);

        // Simuleer exception bij versturen alert
        mockAlertSender.Setup(sender => sender.SendAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP error"));

        // Act & Assert: mag niet throwen
        var ex = await Record.ExceptionAsync(() => sut.ProcessAlertsAsync());
        Assert.Null(ex);
    }

    /// <summary>
    /// AUT-10: Test dat TaskCreator exceptions worden opgevangen.
    /// </summary>
    [Fact(DisplayName = "AUT-10: TaskCreator exception wordt opgevangen en gelogd")]
    public async Task TaskCreatorException_IsHandledAndLogged()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Endpoint: compY | Severity: 3";
        mockLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(new List<string> { log });

        var recipient = new Recipient("UserY", new List<string> { "y@example.com" }, new List<string> { "compY" }, "BoardY", GroupingStrategyType.Severity );
        mockRecipientResolver.Setup(r => r.ResolveRecipient(It.IsAny<Alert>())).Returns(recipient);

        // Simuleer exception bij aanmaken werkitem
        mockTaskCreator.Setup(t => t.TaskExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        mockTaskCreator.Setup(t => t.CreateWorkItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WorkItemType>()))
            .ThrowsAsync(new Exception("Azure DevOps error"));

        // Act & Assert: mag niet throwen
        var ex = await Record.ExceptionAsync(() => sut.ProcessAlertsAsync());
        Assert.Null(ex);
    }

    /// <summary>
    /// AUT-11: Test dat logs correct worden opgehaald van LogProvider.
    /// </summary>
    [Fact(DisplayName = "AUT-11: Logs worden opgehaald van LogProvider")]
    public async Task LogsFetched_FromLogProvider()
    {
        // Arrange
        var logs = new List<string>
        {
            "Timestamp: 2024-06-01T12:00:00Z | Endpoint: compZ | Severity: 3"
        };
        mockLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(logs);

        var recipient = new Recipient("UserZ", new List<string> { "z@example.com" }, new List<string> { "compZ" }, "BoardZ", GroupingStrategyType.Severity );
        var fakeRecipientResolver = new FakeRecipientResolver(new List<Recipient> { recipient });
        var capturingTaskCreator = new CapturingTaskCreator();

        sut = new AlertManager(
            new LogProcessor(mockLogProvider.Object),
            new AlertCreator(),
            mockAlertSender.Object,
            mockSentAlertsStorage.Object,
            alertStrategyManager,
            fakeRecipientResolver,
            capturingTaskCreator
        );

        mockTaskCreator.Setup(t => t.TaskExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        mockTaskCreator.Setup(t => t.CreateWorkItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WorkItemType>()))
            .ReturnsAsync((string?)null);

        // Act
        await sut.ProcessAlertsAsync();

        // Assert: controleer dat FetchLogsAsync is aangeroepen
        mockLogProvider.Verify(lp => lp.FetchLogsAsync(), Times.Once);
        // Controleer dat alert is verstuurd
        var expectedComponentKey = "Fatal";
        mockAlertSender.Verify(sender => sender.SendAlertAsync(
            It.IsAny<string>(),
            recipient.Emails[0],
            expectedComponentKey
        ), Times.Once);
    }

    /// <summary>
    /// AUT-16: Test dat bij meerdere fouten alle kritieke alerts worden verstuurd.
    /// </summary>
    [Fact(DisplayName = "AUT-16: Alle kritieke alerts worden verstuurd bij meerdere fouten")]
    public async Task AllCriticalAlertsSent_WhenMultipleCriticalErrors()
    {
        // Arrange
        var log1 = "Timestamp: 2024-06-01T12:00:00Z | Endpoint: comp1 | Severity: 3";
        var log2 = "Timestamp: 2024-06-01T12:01:00Z | Endpoint: comp2 | Severity: 3";
        var log3 = "Timestamp: 2024-06-01T12:02:00Z | Endpoint: comp3 | Severity: 3";

        var recipient1 = new Recipient("User1", new List<string> { "user1@example.com" }, new List<string> { "comp1" }, "Board1", GroupingStrategyType.Severity );
        var recipient2 = new Recipient("User2", new List<string> { "user2@example.com" }, new List<string> { "comp2" }, "Board2", GroupingStrategyType.Severity );
        var recipient3 = new Recipient("User3", new List<string> { "user3@example.com" }, new List<string> { "comp3" }, "Board3", GroupingStrategyType.Severity );

        var fakeRecipientResolver = new FakeRecipientResolver(new List<Recipient> { recipient1, recipient2, recipient3 });
        var capturingTaskCreator = new CapturingTaskCreator();

        mockLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(new List<string> { log1, log2, log3 });

        sut = new AlertManager(
            new LogProcessor(mockLogProvider.Object),
            new AlertCreator(),
            mockAlertSender.Object,
            mockSentAlertsStorage.Object,
            alertStrategyManager,
            fakeRecipientResolver,
            capturingTaskCreator
        );

        mockTaskCreator.Setup(t => t.TaskExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        mockTaskCreator.Setup(t => t.CreateWorkItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WorkItemType>()))
            .ReturnsAsync((string?)null);

        // Act
        await sut.ProcessAlertsAsync();

        // Assert
        var expectedComponentKey1 = "Fatal";
        var expectedComponentKey2 = "Fatal";
        var expectedComponentKey3 = "Fatal";

        mockAlertSender.Verify(sender => sender.SendAlertAsync(
            It.IsAny<string>(),
            recipient1.Emails[0],
            expectedComponentKey1
        ), Times.Once);

        mockAlertSender.Verify(sender => sender.SendAlertAsync(
            It.IsAny<string>(),
            recipient2.Emails[0],
            expectedComponentKey2
        ), Times.Once);

        mockAlertSender.Verify(sender => sender.SendAlertAsync(
            It.IsAny<string>(),
            recipient3.Emails[0],
            expectedComponentKey3
        ), Times.Once);
    }

    /// <summary>
    /// AUT-17: Test ophalen van logs uit Azure Application Insights (QAS-07).
    /// </summary>
    [Fact(DisplayName = "AUT-17: AzureLogProvider haalt succesvol logs op (mocked)")]
    public async Task AzureLogProvider_FetchesLogs_Mocked()
    {
        // Arrange
        var logs = new List<string> { "Timestamp: 2024-06-01T12:00:00Z | Endpoint: azure-comp | Severity: 3" };
        var mockAzureLogProvider = new Mock<ILogProvider>();
        mockAzureLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(logs);

        var logProcessor = new LogProcessor(mockAzureLogProvider.Object);
        var result = await logProcessor.FetchLogsAsync();

        // Assert
        Assert.Single(result);
        Assert.Contains("azure-comp", result[0]);
    }

    /// <summary>
    /// AUT-18: Test ophalen van logs uit AWS CloudWatch (QAS-08).
    /// </summary>
    [Fact(DisplayName = "AUT-18: AWSLogProvider haalt succesvol logs op (mocked)")]
    public async Task AWSLogProvider_FetchesLogs_Mocked()
    {
        // Arrange
        var logs = new List<string> { "Timestamp: 2024-06-01T12:00:00Z | Endpoint: aws-comp | Severity: 3" };
        var mockAWSLogProvider = new Mock<ILogProvider>();
        mockAWSLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(logs);

        var logProcessor = new LogProcessor(mockAWSLogProvider.Object);
        var result = await logProcessor.FetchLogsAsync();

        // Assert
        Assert.Single(result);
        Assert.Contains("aws-comp", result[0]);
    }

    /// <summary>
    /// AUT-19: Test versturen van een alert via e-mail (QAS-10).
    /// </summary>
    [Fact(DisplayName = "AUT-19: Alert wordt via e-mail verstuurd naar juiste ontvanger (mocked)")]
    public async Task AlertSent_ViaEmail_ToCorrectRecipient()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Component: mail-comp | Severity: 3";
        mockLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(new List<string> { log });

        var recipient = new Recipient(
            "MailUser",
            new List<string> { "mailuser@example.com" },
            new List<string> { "mail-comp" },
            "MailBoard",
            GroupingStrategyType.Severity
        );
        var fakeRecipientResolver = new FakeRecipientResolver(new List<Recipient> { recipient });
        var capturingTaskCreator = new CapturingTaskCreator();

        sut = new AlertManager(
            new LogProcessor(mockLogProvider.Object),
            new AlertCreator(),
            mockAlertSender.Object,
            mockSentAlertsStorage.Object,
            alertStrategyManager,
            fakeRecipientResolver,
            capturingTaskCreator
        );

        mockTaskCreator.Setup(t => t.TaskExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        mockTaskCreator.Setup(t => t.CreateWorkItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WorkItemType>()))
            .ReturnsAsync((string?)null);

        // Act
        await sut.ProcessAlertsAsync();

        // Assert
        var expectedComponentKey = "Fatal";
        mockAlertSender.Verify(sender => sender.SendAlertAsync(
            It.IsAny<string>(),
            recipient.Emails[0],
            expectedComponentKey
        ), Times.Once);
    }

    /// <summary>
    /// AUT-20: Test ophalen en verwerken van logs uit Azure Application Insights (QAS-12).
    /// </summary>
    [Fact(DisplayName = "AUT-20: Logs uit Azure Application Insights worden opgehaald en verwerkt")]
    public async Task AzureLogs_AreFetchedAndProcessed()
    {
        // Arrange
        var logs = new List<string>
        {
            "Timestamp: 2024-06-01T12:00:00Z | Endpoint: azure-app | Severity: 3"
        };
        mockLogProvider.Setup(lp => lp.FetchLogsAsync()).ReturnsAsync(logs);

        var recipient = new Recipient("AzureUser", new List<string> { "azureuser@example.com" }, new List<string> { "azure-app" }, "AzureBoard", GroupingStrategyType.Severity );
        var fakeRecipientResolver = new FakeRecipientResolver(new List<Recipient> { recipient });
        var capturingTaskCreator = new CapturingTaskCreator();

        sut = new AlertManager(
            new LogProcessor(mockLogProvider.Object),
            new AlertCreator(),
            mockAlertSender.Object,
            mockSentAlertsStorage.Object,
            alertStrategyManager,
            fakeRecipientResolver,
            capturingTaskCreator
        );

        mockTaskCreator.Setup(t => t.TaskExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        mockTaskCreator.Setup(t => t.CreateWorkItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WorkItemType>()))
            .ReturnsAsync((string?)null);

        // Act
        await sut.ProcessAlertsAsync();

        // Assert
        var expectedComponentKey = "Fatal";
        mockAlertSender.Verify(sender => sender.SendAlertAsync(
            It.IsAny<string>(),
            recipient.Emails[0],
            expectedComponentKey
        ), Times.Once);
        mockLogProvider.Verify(lp => lp.FetchLogsAsync(), Times.Once);
    }

    public class FakeRecipientResolver : IRecipientResolver, IRecipientListProvider
    {
        public List<Recipient> Recipients { get; }
        public FakeRecipientResolver(List<Recipient> recipients)
        {
            Recipients = recipients;
        }
        public Recipient? ResolveRecipient(Alert alert)
        {
            return Recipients.FirstOrDefault(r =>
                r.ResponsibleComponents.Any(c => string.Equals(c, alert.Component, StringComparison.OrdinalIgnoreCase)));
        }
        public IEnumerable<Recipient> GetAllRecipients() => Recipients;
    }
}
