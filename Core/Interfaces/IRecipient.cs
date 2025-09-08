using System;
using System.Collections.Generic;

public interface IRecipient
{
    string Name { get; set; }
    List<string> Emails { get; set; }
    List<string> ResponsibleComponents { get; set; }
    string Board { get; set; }
    GroupingStrategyType GroupingStrategy { get; set; }
}
