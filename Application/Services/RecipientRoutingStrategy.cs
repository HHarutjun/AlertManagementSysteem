using System;
using System.Collections.Generic;
using VanDijk.AlertManagement.Core.Interfaces;

public class RecipientRoutingStrategy : IRoutingStrategy, IRecipientListProvider
{
    private readonly List<Recipient> _recipients;

    public RecipientRoutingStrategy(IList<Recipient> recipients)
    {
        _recipients = new List<Recipient>(recipients ?? throw new ArgumentNullException(nameof(recipients), "Recipients configuration cannot be null."));
    }

    public Recipient? DetermineRecipient(Alert alert)
    {
        if (alert == null)
            throw new ArgumentNullException(nameof(alert), "Alert cannot be null.");

        if (string.IsNullOrWhiteSpace(alert.Component))
            throw new ArgumentException("Alert component cannot be null or empty.", nameof(alert.Component));

        Console.WriteLine($"[Debug] DetermineRecipient: alert.Component = '{alert.Component}'");
        foreach (var recipient in _recipients)
        {
            Console.WriteLine($"[Debug] Checking recipient '{recipient.Name}'");
            if (recipient.ResponsibleComponents == null)
            {
                Console.WriteLine($"[Debug] Recipient '{recipient.Name}' has null ResponsibleComponents!");
                continue;
            }
            Console.WriteLine($"[Debug] ResponsibleComponents count: {recipient.ResponsibleComponents.Count}");
            foreach (var rc in recipient.ResponsibleComponents)
            {
                Console.WriteLine($"[Debug] ResponsibleComponent: '{rc}'");
                if (string.Equals(
                        (rc ?? "").Trim().Trim('"'),
                        (alert.Component ?? "").Trim().Trim('"'),
                        StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[Debug] MATCH: '{alert.Component}' matched with '{recipient.Name}' (ResponsibleComponent: '{rc}')");
                    return recipient;
                }
            }
        }

        Console.WriteLine($"[Warning] No recipient found for component: {alert.Component}");
        return null;
    }

    // Nieuw: geef alle recipients terug die verantwoordelijk zijn voor een component
    public virtual IEnumerable<Recipient> GetRecipientsForComponent(string component)
    {
        if (string.IsNullOrEmpty(component))
        {
            foreach (var recipient in _recipients)
            {
                yield return recipient;
            }
            yield break;
        }

        foreach (var recipient in _recipients)
        {
            if (recipient.ResponsibleComponents != null &&
                recipient.ResponsibleComponents.Exists(c => string.Equals(c, component, StringComparison.OrdinalIgnoreCase)))
            {
                yield return recipient;
            }
        }
    }

    public void UpdateRecipients(IList<Recipient> recipients)
    {
        _recipients.Clear();
        _recipients.AddRange(recipients);
    }

    public IEnumerable<Recipient> GetAllRecipients()
    {
        return _recipients;
    }
}
