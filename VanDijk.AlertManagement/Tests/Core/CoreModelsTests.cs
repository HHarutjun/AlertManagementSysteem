using System;
using System.Text.Json;
using Xunit;
using VanDijk.AlertManagement.Core.Parsers;

public class CoreModelsTests
{
    /// <summary>
    /// AUT-37: Test Alert properties en methods coverage
    /// </summary>
    [Fact(DisplayName = "AUT-37: Alert properties en methods coverage")]
    public void Alert_AllPropertiesAndMethods_AreCovered()
    {
        var alert = new Alert
        {
            Message = "Test message",
            Severity = "Fatal",
            Timestamp = DateTime.UtcNow,
            Component = "compA",
            Status = AlertStatus.New
        };

        Assert.Equal("Test message", alert.Message);
        Assert.Equal("Fatal", alert.Severity);
        Assert.Equal("compA", alert.Component);
        Assert.Equal(AlertStatus.New, alert.Status);

        // Test MarkInProgress en MarkResolved
        alert.MarkInProgress();
        Assert.Equal(AlertStatus.InProgress, alert.Status);

        alert.MarkResolved();
        Assert.Equal(AlertStatus.Resolved, alert.Status);

        // Test interface setter
        IAlert ialert = alert;
        ialert.Status = "New";
        Assert.Equal(AlertStatus.New, alert.Status);

        Assert.Throws<ArgumentException>(() => ialert.Status = "NotAStatus");
    }

    /// <summary>
    /// AUT-38: Test IterationListResponse en Iteration coverage
    /// </summary>
    [Fact(DisplayName = "AUT-38: IterationListResponse en Iteration coverage")]
    public void IterationListResponse_And_Iteration_AreCovered()
    {
        var iteration = new Iteration { Path = "Development\\Sheldon\\Sprint 1" };
        Assert.Equal("Development\\Sheldon\\Sprint 1", iteration.Path);

        var response = new TestIterationListResponse
        {
            Value = new System.Collections.Generic.List<Iteration> { iteration }
        };
        Assert.Single(response.Value);
        Assert.Equal("Development\\Sheldon\\Sprint 1", response.Value[0].Path);
    
        // Test serialisatie/deserialisatie
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<TestIterationListResponse>(json);
        Assert.Single(deserialized.Value);
        Assert.Equal("Development\\Sheldon\\Sprint 1", deserialized.Value[0].Path);
    }

    /// <summary>
    /// AUT-39: Test LogEntry properties coverage
    /// </summary>
    [Fact(DisplayName = "AUT-39: LogEntry properties coverage")]
    public void LogEntry_AllProperties_AreCovered()
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Severity = "Fatal",
            Component = "compA",
            Message = "Test log"
        };

        Assert.Equal("Fatal", logEntry.Severity);
        Assert.Equal("compA", logEntry.Component);
        Assert.Equal("Test log", logEntry.Message);
        Assert.True((DateTime.UtcNow - logEntry.Timestamp).TotalSeconds < 5);
    }

    /// <summary>
    /// AUT-40: Test LogParser.Parse coverage
    /// </summary>
    [Fact(DisplayName = "AUT-40: LogParser.Parse coverage")]
    public void LogParser_Parse_ReturnsLogEntry()
    {
        var parser = new LogParser();
        var now = DateTime.UtcNow;
        var log = $"{now:yyyy-MM-ddTHH:mm:ss}|Fatal|compA|Test log message";
        var entry = parser.Parse(log);

        // Vergelijk met tolerantie van 1 seconde
        Assert.True(Math.Abs((now - entry.Timestamp).TotalSeconds) < 1, $"Expected: {now}, Actual: {entry.Timestamp}");
        Assert.Equal("Fatal", entry.Severity);
        Assert.Equal("compA", entry.Component);
        Assert.Equal("Test log message", entry.Message);
    }
}
