namespace VanDijk.AlertManagement.Core.Interfaces
{
    /// <summary>
    /// Defines a strategy for determining the recipient of an alert.
    /// </summary>
    public interface IRoutingStrategy
    {
        /// <summary>
        /// Determines the recipient for the specified alert.
        /// </summary>
        /// <param name="alert">The alert to determine the recipient for.</param>
        /// <returns>The determined recipient, or null if no recipient is found.</returns>
        Recipient? DetermineRecipient(Alert alert);
    }
}
