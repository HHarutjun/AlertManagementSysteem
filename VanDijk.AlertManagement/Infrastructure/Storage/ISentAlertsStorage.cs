using System.Collections.Generic;

/// <summary>
/// Provides methods to load and save sent alerts.
/// </summary>
public interface ISentAlertsStorage
{
    /// <summary>
    /// Loads the set of sent alerts.
    /// </summary>
    /// <returns>A set of alert identifiers that have been sent.</returns>
    ISet<string> LoadSentAlerts();

    /// <summary>
    /// Saves the set of sent alerts.
    /// </summary>
    /// <param name="sentAlerts">A set of alert identifiers to save as sent.</param>
    void SaveSentAlerts(ISet<string> sentAlerts);
}