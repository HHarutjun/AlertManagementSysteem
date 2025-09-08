using System;
using System.Collections.Generic;

public class AlertStrategyManager
{
    private readonly List<IAlertGroupingStrategy> _strategies;

    public AlertStrategyManager(IEnumerable<IAlertGroupingStrategy> initialStrategies)
    {
        _strategies = new List<IAlertGroupingStrategy>(initialStrategies ?? throw new ArgumentNullException(nameof(initialStrategies)));
    }

    public IEnumerable<IAlertGroupingStrategy> GetStrategies() => _strategies;

    public void AddStrategy(IAlertGroupingStrategy strategy)
    {
        if (strategy == null) throw new ArgumentNullException(nameof(strategy));
        _strategies.Add(strategy);
        Console.WriteLine($"[Info] Added new alert strategy: {strategy.GetType().Name}");
    }

    public void RemoveStrategy(IAlertGroupingStrategy strategy)
    {
        if (strategy == null) throw new ArgumentNullException(nameof(strategy));
        _strategies.Remove(strategy);
        Console.WriteLine($"[Info] Removed alert strategy: {strategy.GetType().Name}");
    }
}
