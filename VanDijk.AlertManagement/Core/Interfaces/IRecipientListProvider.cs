using System.Collections.Generic;

/// <summary>
/// Provides a method to retrieve all recipients.
/// </summary>
public interface IRecipientListProvider
{
    /// <summary>
    /// Gets all recipients.
    /// </summary>
    /// <returns>An enumerable collection of recipients.</returns>
    IEnumerable<Recipient> GetAllRecipients();
}
