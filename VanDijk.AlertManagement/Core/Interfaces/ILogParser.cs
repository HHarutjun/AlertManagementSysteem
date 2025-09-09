/// <summary>
/// Provides a method to parse log entries.
/// </summary>
public interface ILogParser
{
    /// <summary>
    /// Parses a log string and returns a LogEntry object.
    /// </summary>
    /// <param name="log">The log string to parse.</param>
    /// <returns>A LogEntry object representing the parsed log.</returns>
    LogEntry Parse(string log);
}