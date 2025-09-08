using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System;
using System.Linq;

public class RecipientsModel : PageModel
{
    public List<string> GroupingStrategies { get; set; } = new();

    public void OnGet()
    {
        GroupingStrategies = Enum.GetNames(typeof(GroupingStrategyType)).ToList();
    }
}
