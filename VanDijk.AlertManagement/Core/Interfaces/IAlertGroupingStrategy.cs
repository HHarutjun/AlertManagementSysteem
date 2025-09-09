using System.Collections.Generic;

/// <summary>
/// Defines a strategy for grouping alert logs.
/// </summary>
public interface IAlertGroupingStrategy
{
    /// <summary>
    /// Groups the provided logs according to the implemented strategy.
    /// </summary>
    /// <param name="logs">A list of log entries to group.</param>
    /// <returns>A dictionary where the key is the group identifier and the value is a list of log entries in that group.</returns>
    IDictionary<string, IList<string>> GroupLogs(IList<string> logs);
}
