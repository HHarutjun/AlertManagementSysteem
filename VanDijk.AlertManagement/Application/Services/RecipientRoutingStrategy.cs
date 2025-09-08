using System;
using System.Collections.Generic;
using VanDijk.AlertManagement.Core.Interfaces;

/// <summary>
/// Provides routing logic to determine the appropriate recipient(s) for a given alert based on responsible components.
/// </summary>
public class RecipientRoutingStrategy : IRoutingStrategy, IRecipientListProvider
{
    private readonly List<Recipient> recipients;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecipientRoutingStrategy"/> class with the specified recipients.
    /// </summary>
    /// <param name="recipients">The list of recipients to use for routing.</param>
    /// <exception cref="ArgumentNullException">Thrown when the recipients parameter is null.</exception>
    public RecipientRoutingStrategy(IList<Recipient> recipients)
    {
        this.recipients = new List<Recipient>(recipients ?? throw new ArgumentNullException(nameof(recipients), "Recipients configuration cannot be null."));
    }

    /// <summary>
    /// Determines the appropriate recipient for the specified alert based on the alert's component.
    /// </summary>
    /// <param name="alert">The alert for which to determine the recipient.</param>
    /// <returns>The recipient responsible for the alert's component, or null if no match is found.</returns>
    public Recipient? DetermineRecipient(Alert alert)
    {
        if (alert == null)
        {
            throw new ArgumentNullException(nameof(alert), "Alert cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(alert.Component))
        {
            throw new ArgumentException("Alert component cannot be null or empty.", nameof(alert.Component));
        }

        Console.WriteLine($"[Debug] DetermineRecipient: alert.Component = '{alert.Component}'");
        foreach (var recipient in this.recipients)
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
                        (rc ?? string.Empty).Trim().Trim('"'),
                        (alert.Component ?? string.Empty).Trim().Trim('"'),
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

    /// <summary>
    /// Returns all recipients responsible for the specified component.
    /// If the component is null or empty, all recipients are returned.
    /// </summary>
    /// <param name="component">The component to find responsible recipients for.</param>
    /// <returns>An enumerable of recipients responsible for the given component.</returns>
    public virtual IEnumerable<Recipient> GetRecipientsForComponent(string component)
    {
        if (string.IsNullOrEmpty(component))
        {
            foreach (var recipient in this.recipients)
            {
                yield return recipient;
            }

            yield break;
        }

        foreach (var recipient in this.recipients)
        {
            if (recipient.ResponsibleComponents != null &&
                recipient.ResponsibleComponents.Exists(c => string.Equals(c, component, StringComparison.OrdinalIgnoreCase)))
            {
                yield return recipient;
            }
        }
    }

    /// <summary>
    /// Updates the list of recipients used for routing.
    /// </summary>
    /// <param name="recipients">The new list of recipients to use.</param>
    public void UpdateRecipients(IList<Recipient> recipients)
    {
        this.recipients.Clear();
        this.recipients.AddRange(recipients);
    }

    /// <summary>
    /// Returns all recipients currently used for routing.
    /// </summary>
    /// <returns>An enumerable of all recipients.</returns>
    public IEnumerable<Recipient> GetAllRecipients()
    {
        return this.recipients;
    }
}
