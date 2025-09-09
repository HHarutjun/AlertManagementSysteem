using System;
using System.Collections.Generic;

/// <summary>
/// Represents a recipient with contact information and grouping strategy.
/// </summary>
public interface IRecipient
{
    /// <summary>
    /// Gets or sets the name of the recipient.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the list of email addresses for the recipient.
    /// </summary>
    List<string> Emails { get; set; }

    /// <summary>
    /// Gets or sets the list of components the recipient is responsible for.
    /// </summary>
    List<string> ResponsibleComponents { get; set; }

    /// <summary>
    /// Gets or sets the board associated with the recipient.
    /// </summary>
    string Board { get; set; }

    /// <summary>
    /// Gets or sets the grouping strategy type for the recipient.
    /// </summary>
    GroupingStrategyType GroupingStrategy { get; set; }
}
