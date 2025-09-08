using System;
using System.Collections.Generic;

/// <summary>
/// Factory class for creating alert grouping strategies based on the specified strategy type.
/// </summary>
public class AlertGroupingStrategyFactory
{
    /// <summary>
    /// Creates an alert grouping strategy based on the specified strategy type.
    /// </summary>
    /// <param name="strategyType">The type of grouping strategy to create.</param>
    /// <param name="severity">The severity level to use, or null to use the default.</param>
    /// <param name="timeRange">The optional time range for grouping.</param>
    /// <param name="getKnownProblemsForComponent">A function to retrieve known problems for a component, or null for default.</param>
    /// <returns>An instance of <see cref="IAlertGroupingStrategy"/> based on the specified strategy type.</returns>
    public static IAlertGroupingStrategy CreateStrategy(GroupingStrategyType strategyType, string? severity = null, TimeSpan? timeRange = null, Func<string, HashSet<string>>? getKnownProblemsForComponent = null)
    {
        var effectiveSeverity = severity ?? AlertSeverity.Fatal.ToString();
        var knownProblemsFunc = getKnownProblemsForComponent ?? (Func<string, HashSet<string>>)(_ => new HashSet<string>());
        return strategyType switch
        {
            GroupingStrategyType.Component => new CorrelateByComponentStrategy(),
            GroupingStrategyType.ExceptionType => new CorrelateByExceptionTypeStrategy(),
            GroupingStrategyType.Severity => new CorrelateBySeverityStrategy(),
            GroupingStrategyType.KnownProblemsFilter => new KnownProblemsFilterStrategy(knownProblemsFunc),
            _ => throw new NotImplementedException($"Strategy {strategyType} not implemented.")
        };
    }
}
