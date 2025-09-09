/// <summary>
/// Specifies the strategy types for grouping alerts.
/// </summary>
public enum GroupingStrategyType
{
    /// <summary>
    /// Group alerts by component.
    /// </summary>
    Component,

    /// <summary>
    /// Group alerts by severity.
    /// </summary>
    Severity,

    /// <summary>
    /// Group alerts by exception type.
    /// </summary>
    ExceptionType,

    /// <summary>
    /// Group alerts by known problems filter.
    /// </summary>
    KnownProblemsFilter,
}
