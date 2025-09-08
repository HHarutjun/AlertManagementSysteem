using System;
using Xunit;

// ...eventueel benodigde using statements voor Alert, AlertCreator, AlertStatus...

public class AlertCreatorTests
{
    /// <summary>
    /// AUT-12: Test dat AlertCreator correcte Alert aanmaakt.
    /// </summary>
    [Fact(DisplayName = "AUT-12: AlertCreator maakt correcte Alert aan")]
    public void AlertCreator_CreatesCorrectAlert()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var log = $"Timestamp: {now:yyyy-MM-ddTHH:mm:ssZ} | Endpoint: compA | Severity: 3";
        var component = "compA";
        var alertCreator = new AlertCreator();

        // Act
        var alert = alertCreator.CreateAlert(log, component);

        // Assert
        Assert.NotNull(alert);
        Assert.Equal(log, alert.Message);
        Assert.Equal("Fatal", alert.Severity);
        Assert.Equal(component, alert.Component);
        Assert.Equal(AlertStatus.New, alert.Status);
        Assert.True(Math.Abs((now - alert.Timestamp).TotalSeconds) < 1, $"Expected: {now}, Actual: {alert.Timestamp}");
    }

    /// <summary>
    /// AUT-13: Test dat een alert de juiste tijd, locatie, beschrijving en impact bevat.
    /// </summary>
    [Fact(DisplayName = "AUT-13: Alert bevat juiste tijd, component, beschrijving en impact")]
    public void Alert_HasCorrectDetails_FromLog()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var log = $"Timestamp: {timestamp} | Endpoint: vdl-catalogus-neu | Status: Down | Impact: High | Description: Database unreachable";
        var component = "vdl-catalogus-neu";
        var alertCreator = new AlertCreator();

        // Act
        var alert = alertCreator.CreateAlert(log, component);

        // Assert
        Assert.NotNull(alert);
        Assert.Contains("vdl-catalogus-neu", alert.Message);
        Assert.Equal(component, alert.Component);
        Assert.Equal("Fatal", alert.Severity);
        Assert.Equal(AlertStatus.New, alert.Status);
        Assert.True((DateTime.UtcNow - alert.Timestamp).TotalSeconds < 5);
        Assert.Contains("Impact: High", alert.Message);
        Assert.Contains("Description: Database unreachable", alert.Message);
    }

    /// <summary>
    /// AUT-15: Test dat een alert de juiste applicatie-informatie bevat.
    /// </summary>
    [Fact(DisplayName = "AUT-15: Alert bevat juiste applicatie-informatie")]
    public void Alert_HasCorrectApplicationInfo()
    {
        // Arrange
        var log = "Timestamp: 2024-06-01T12:00:00Z | Endpoint: my-app | Status: Down | Application: MyApp";
        var component = "my-app";
        var alertCreator = new AlertCreator();

        // Act
        var alert = alertCreator.CreateAlert(log, component);

        // Assert
        Assert.NotNull(alert);
        Assert.Contains("Application: MyApp", alert.Message);
        Assert.Equal(component, alert.Component);
    }
}
