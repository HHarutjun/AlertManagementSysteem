using System;
using System.Collections.Generic;
using System.Linq;
using VanDijk.AlertManagement.Core.Interfaces;

public class RecipientResolver : IRecipientResolver, IRecipientListProvider
{
    private readonly IRoutingStrategy _routingStrategy;
    private readonly RecipientStorage _recipientStorage;

    public RecipientResolver(IRoutingStrategy routingStrategy, RecipientStorage recipientStorage)
    {
        _routingStrategy = routingStrategy;
        _recipientStorage = recipientStorage;
    }

    public void RefreshRecipients()
    {
        var recipients = _recipientStorage.LoadRecipients();
        if (_routingStrategy is RecipientRoutingStrategy rrs)
        {
            rrs.UpdateRecipients(recipients);
        }
    }

    public Recipient? ResolveRecipient(Alert alert)
    {
        RefreshRecipients();
        return _routingStrategy.DetermineRecipient(alert);
    }

    public IRoutingStrategy RoutingStrategy => _routingStrategy;

    public IEnumerable<Recipient> GetAllRecipients()
    {
        if (_routingStrategy is RecipientRoutingStrategy rrs)
            return rrs.GetRecipientsForComponent(null);
        return Enumerable.Empty<Recipient>();
    }
}
