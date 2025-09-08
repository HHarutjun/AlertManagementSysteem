namespace VanDijk.AlertManagement.Core.Interfaces
{
    public interface IRoutingStrategy
    {
        Recipient? DetermineRecipient(Alert alert);
    }
}
