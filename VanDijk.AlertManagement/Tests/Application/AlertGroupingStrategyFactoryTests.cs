using System;
using Xunit;

public class AlertGroupingStrategyFactoryTests
{
    /// <summary>
    /// AUT-23: Test aanmaken van CorrelateByComponentStrategy.
    /// </summary>
    [Fact(DisplayName = "AUT-23: Maakt CorrelateByComponentStrategy aan bij type Component")]
    public void CreateStrategy_Component_ReturnsCorrelateByComponentStrategy()
    {
        // Act
        var strategy = AlertGroupingStrategyFactory.CreateStrategy(GroupingStrategyType.Component);

        // Assert
        Assert.NotNull(strategy);
        Assert.IsType<CorrelateByComponentStrategy>(strategy);
    }

    /// <summary>
    /// AUT-24: Test aanmaken van CorrelateBySeverityStrategy.
    /// </summary>
    [Fact(DisplayName = "AUT-24: Maakt CorrelateBySeverityStrategy aan bij type SeverityAndTime")]
    public void CreateStrategy_Correlation_ReturnsCorrelateBySeverityStrategy()
    {
        // Act
        var strategy = AlertGroupingStrategyFactory.CreateStrategy(GroupingStrategyType.Severity, "Fatal", TimeSpan.FromHours(1));

        // Assert
        Assert.NotNull(strategy);
        Assert.IsType<CorrelateBySeverityStrategy>(strategy);
    }

    /// <summary>
    /// AUT-25: Test exception bij onbekende strategy.
    /// </summary>
    [Fact(DisplayName = "AUT-25: Gooit NotImplementedException bij onbekend type")]
    public void CreateStrategy_UnknownType_Throws()
    {
        // Act & Assert
        Assert.Throws<NotImplementedException>(() => AlertGroupingStrategyFactory.CreateStrategy((GroupingStrategyType)999));
    }
}
