using System;
using System.Collections.Generic;
using Xunit;

public class AlertStrategyAndFactoryTests
{
    [Fact]
    public void AlertStrategyManager_AddRemove_Null_Throws()
    {
        var mgr = new AlertStrategyManager(new List<IAlertGroupingStrategy>());
        Assert.Throws<ArgumentNullException>(() => mgr.AddStrategy(null));
        Assert.Throws<ArgumentNullException>(() => mgr.RemoveStrategy(null));
    }

    [Fact]
    public void AlertStrategyManager_Constructor_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AlertStrategyManager(null));
    }

    [Fact]
    public void AlertGroupingStrategyFactory_AllBranches()
    {
        Assert.IsType<CorrelateByComponentStrategy>(AlertGroupingStrategyFactory.CreateStrategy(GroupingStrategyType.Component));
        Assert.IsType<CorrelateBySeverityStrategy>(AlertGroupingStrategyFactory.CreateStrategy(GroupingStrategyType.Severity));
        Assert.IsType<CorrelateByExceptionTypeStrategy>(AlertGroupingStrategyFactory.CreateStrategy(GroupingStrategyType.ExceptionType));
        Assert.Throws<NotImplementedException>(() => AlertGroupingStrategyFactory.CreateStrategy((GroupingStrategyType)999));
    }

    [Fact]
    public void KnownProblemsFilterStrategy_FiltersKnownProblems()
    {
        var knownProblems = new HashSet<string> { "KnownProblem" };
        var logs = new List<string>
        {
            "Timestamp: 2024-06-01T12:00:00Z | ProblemId: KnownProblem",
            "Timestamp: 2024-06-01T12:00:00Z | ProblemId: UnknownProblem"
        };
        var strat = new KnownProblemsFilterStrategy(_ => knownProblems);
        var grouped = strat.GroupLogs(logs);
        Assert.Single(grouped);
        Assert.Contains("Unknown", grouped.Keys);
    }

    [Fact]
    public void CorrelateByExceptionTypeStrategy_GroupsByExceptionType()
    {
        var logs = new List<string>
        {
            "Timestamp: 2024-06-01T12:00:00Z | ExceptionType: System.Exception",
            "Timestamp: 2024-06-01T12:00:00Z | ExceptionType: System.Exception",
            "Timestamp: 2024-06-01T12:00:00Z | ExceptionType: System.IO.IOException"
        };
        var strat = new CorrelateByExceptionTypeStrategy();
        var grouped = strat.GroupLogs(logs);
        Assert.Equal(2, grouped.Count);
        Assert.Contains("System.Exception", grouped.Keys);
        Assert.Contains("System.IO.IOException", grouped.Keys);
    }
}
