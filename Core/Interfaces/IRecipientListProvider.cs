using System.Collections.Generic;

public interface IRecipientListProvider
{
    IEnumerable<Recipient> GetAllRecipients();
}
