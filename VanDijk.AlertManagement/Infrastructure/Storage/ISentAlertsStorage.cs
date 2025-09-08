using System.Collections.Generic;

public interface ISentAlertsStorage
{
    ISet<string> LoadSentAlerts();
    void SaveSentAlerts(ISet<string> sentAlerts);
}