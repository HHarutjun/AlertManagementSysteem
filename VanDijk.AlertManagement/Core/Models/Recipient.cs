using System.Collections.Generic;

public class Recipient : IRecipient
{
    public string Name { get; set; }
    public List<string> Emails { get; set; }
    public List<string> ResponsibleComponents { get; set; }
    public string Board { get; set; }
    public GroupingStrategyType GroupingStrategy { get; set; }

    public Recipient(string name, List<string> emails, List<string> responsibleComponents, string board, GroupingStrategyType groupingStrategy = GroupingStrategyType.Component)
    {
        Name = name;
        Emails = emails;
        ResponsibleComponents = responsibleComponents;
        Board = board;
        GroupingStrategy = groupingStrategy;
    }

    public override string ToString()
    {
        return $"Name={Name}, Emails=[{string.Join(",", Emails)}], ResponsibleComponents=[{string.Join(",", ResponsibleComponents)}], Board={Board}, GroupingStrategy={GroupingStrategy}";
    }
}
