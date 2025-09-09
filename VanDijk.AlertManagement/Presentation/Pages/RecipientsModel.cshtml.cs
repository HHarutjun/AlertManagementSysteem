using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Page model for managing recipients and grouping strategies.
/// </summary>
public class RecipientsModel : PageModel
{
    /// <summary>
    /// Gets or sets the list of grouping strategies available for recipients.
    /// </summary>
    public List<string> GroupingStrategies { get; set; } = new ();

    /// <summary>
    /// Handles GET requests and initializes the list of grouping strategies.
    /// </summary>
    public void OnGet()
    {
        this.GroupingStrategies = Enum.GetNames(typeof(GroupingStrategyType)).ToList();
    }
}
