using VanDijk.AlertManagement.Core.Interfaces;

/// <summary>
/// Defines a method to resolve a recipient for a given alert.
/// </summary>
public interface IRecipientResolver
{
    /// <summary>
    /// Resolves the recipient for the specified alert.
    /// </summary>
    /// <param name="alert">The alert for which to resolve the recipient.</param>
    /// <returns>The resolved recipient.</returns>
    Recipient ResolveRecipient(Alert alert);
}
