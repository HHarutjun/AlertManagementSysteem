using System.Collections.Generic;

/// <summary>
/// Represents a recipient with contact information and grouping strategy.
/// </summary>
public class Recipient : IRecipient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Recipient"/> class with the specified details.
    /// </summary>
    /// <param name="name">The name of the recipient.</param>
    /// <param name="emails">The list of email addresses for the recipient.</param>
    /// <param name="responsibleComponents">The components for which the recipient is responsible.</param>
    /// <param name="board">The board associated with the recipient.</param>
    /// <param name="groupingStrategy">The grouping strategy for the recipient.</param>
    public Recipient(string name, List<string> emails, List<string> responsibleComponents, string board, GroupingStrategyType groupingStrategy = GroupingStrategyType.Component)
    {
        this.Name = name;
        this.Emails = emails;
        this.ResponsibleComponents = responsibleComponents;
        this.Board = board;
        this.GroupingStrategy = groupingStrategy;
    }

    /// <summary>
    /// Gets or sets the name of the recipient.
    /// </summary>
    /// <summary>
    /// Gets or sets the email addresses of the recipient.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the email addresses of the recipient.
    /// </summary>
    public List<string> Emails { get; set; }
    /// <summary>
    /// Gets or sets the list of components for which the recipient is responsible.
    /// </summary>
    public List<string> ResponsibleComponents { get; set; }
    /// <summary>
    /// Gets or sets the board associated with the recipient.
    /// </summary>
    public string Board { get; set; }
    /// <summary>
    /// Gets or sets the grouping strategy for the recipient.
    /// </summary>
    public GroupingStrategyType GroupingStrategy { get; set; }

       /// <summary>
       /// Returns a string that represents the current Recipient object.
       /// </summary>
       /// <returns>A string representation of the Recipient.</returns>
    public override string ToString()
    {
        return $"Name={this.Name}, Emails=[{string.Join(",", this.Emails)}], ResponsibleComponents=[{string.Join(",", this.ResponsibleComponents)}], Board={this.Board}, GroupingStrategy={this.GroupingStrategy}";
    }
}
