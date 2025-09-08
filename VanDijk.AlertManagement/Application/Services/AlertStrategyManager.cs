using System;
using System.Collections.Generic;

/// <summary>
/// Manages a collection of alert grouping strategies, allowing addition and removal of strategies.
/// </summary>
public class AlertStrategyManager
{
    private readonly List<IAlertGroupingStrategy> strategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlertStrategyManager"/> class with the specified initial strategies.
    /// </summary>
    /// <param name="initialStrategies">The initial collection of alert grouping strategies.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="initialStrategies"/> is null.</exception>
    public AlertStrategyManager(IEnumerable<IAlertGroupingStrategy> initialStrategies)
    {
        this.strategies = new List<IAlertGroupingStrategy>(initialStrategies ?? throw new ArgumentNullException(nameof(initialStrategies)));
    }

    /// <summary>
    /// Gets the collection of alert grouping strategies currently managed.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="IAlertGroupingStrategy"/>.</returns>
    public IEnumerable<IAlertGroupingStrategy> GetStrategies() => this.strategies;

    /// <summary>
    /// Adds a new alert grouping strategy to the manager.
    /// </summary>
    /// <param name="strategy">The alert grouping strategy to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="strategy"/> is null.</exception>
    public void AddStrategy(IAlertGroupingStrategy strategy)
    {
        if (strategy == null)
        {
            throw new ArgumentNullException(nameof(strategy));
        }

        this.strategies.Add(strategy);
        Console.WriteLine($"[Info] Added new alert strategy: {strategy.GetType().Name}");
    }

    /// <summary>
    /// Removes an alert grouping strategy from the manager.
    /// </summary>
    /// <param name="strategy">The alert grouping strategy to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="strategy"/> is null.</exception>
    public void RemoveStrategy(IAlertGroupingStrategy strategy)
    {
        if (strategy == null)
        {
            throw new ArgumentNullException(nameof(strategy));
        }

        this.strategies.Remove(strategy);
        Console.WriteLine($"[Info] Removed alert strategy: {strategy.GetType().Name}");
    }
}
