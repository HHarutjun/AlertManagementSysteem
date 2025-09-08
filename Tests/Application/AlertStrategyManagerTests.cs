using System;
using System.Collections.Generic;
using Xunit;

public class AlertStrategyManagerTests
{
    /// <summary>
    /// AUT-26: Test toevoegen van strategie.
    /// </summary>
    [Fact(DisplayName = "AUT-26: Strategie wordt toegevoegd")]
    public void AddStrategy_StrategyIsAdded()
    {
        var manager = new AlertStrategyManager(new List<IAlertGroupingStrategy>());
        var strategy = new CorrelateByComponentStrategy();

        manager.AddStrategy(strategy);

        Assert.Contains(strategy, manager.GetStrategies());
    }

    /// <summary>
    /// AUT-27: Test verwijderen van strategie.
    /// </summary>
    [Fact(DisplayName = "AUT-27: Strategie wordt verwijderd")]
    public void RemoveStrategy_StrategyIsRemoved()
    {
        var strategy = new CorrelateByComponentStrategy();
        var manager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { strategy });

        manager.RemoveStrategy(strategy);

        Assert.DoesNotContain(strategy, manager.GetStrategies());
    }

    /// <summary>
    /// AUT-28: Test ophalen van strategieën.
    /// </summary>
    [Fact(DisplayName = "AUT-28: GetStrategies retourneert juiste lijst")]
    public void GetStrategies_ReturnsExpectedList()
    {
        var strategy1 = new CorrelateByComponentStrategy();
        var strategy2 = new CorrelateBySeverityStrategy();
        var manager = new AlertStrategyManager(new List<IAlertGroupingStrategy> { strategy1, strategy2 });

        var strategies = manager.GetStrategies();

        Assert.Contains(strategy1, strategies);
        Assert.Contains(strategy2, strategies);
    }
}
