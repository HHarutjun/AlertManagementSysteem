using VanDijk.AlertManagement.Core.Interfaces;

public interface IRecipientResolver
{
    Recipient ResolveRecipient(Alert alert);
}
