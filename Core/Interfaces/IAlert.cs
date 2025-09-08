using System;

public interface IAlert
{
    string Message { get; set; }
    string Severity { get; set; }
    DateTime Timestamp { get; set; }
    string Component { get; set; }
    string Status { get; set; }

    void MarkInProgress();
    void MarkResolved();
}
