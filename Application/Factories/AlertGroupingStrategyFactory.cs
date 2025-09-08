using System;
using System.Collections.Generic;

public class AlertGroupingStrategyFactory
{
    public static IAlertGroupingStrategy CreateStrategy(GroupingStrategyType strategyType, string severity = null, TimeSpan? timeRange = null, Func<string, HashSet<string>> getKnownProblemsForComponent = null)
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
