using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.IO;

[ApiController]
[Route("api/[controller]")]
public class RecipientsController : ControllerBase
{
    private readonly RecipientStorage _recipientStorage;

    public RecipientsController(RecipientStorage recipientStorage)
    {
        _recipientStorage = recipientStorage;
    }

    [HttpGet]
    public ActionResult<List<object>> GetAll()
    {
        // Toon groupingStrategy als string in de API response
        var recipients = _recipientStorage.LoadRecipients();
        var result = recipients.Select(r => new {
            name = r.Name,
            emails = r.Emails,
            responsibleComponents = r.ResponsibleComponents,
            board = r.Board,
            groupingStrategy = r.GroupingStrategy.ToString()
        }).ToList();
        return Ok(result);
    }

    [HttpGet("grouping-strategies")]
    public ActionResult<List<string>> GetGroupingStrategies()
    {
        return System.Enum.GetNames(typeof(GroupingStrategyType)).ToList();
    }

    public class AddRecipientRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public List<string> ResponsibleComponents { get; set; }
        public string Board { get; set; }
        public GroupingStrategyType GroupingStrategy { get; set; }
    }

    [HttpPost]
    public IActionResult AddRecipient([FromBody] AddRecipientRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName) ||
            string.IsNullOrWhiteSpace(req.Email) || req.ResponsibleComponents == null || req.ResponsibleComponents.Count == 0 ||
            string.IsNullOrWhiteSpace(req.Board))
        {
            return BadRequest("All fields are required.");
        }

        var recipients = _recipientStorage.LoadRecipients();
        var name = $"{req.FirstName} {req.LastName}";

        var board = req.Board.StartsWith("Development\\", StringComparison.OrdinalIgnoreCase)
            ? req.Board
            : $"Development\\{req.Board}";

        var newRecipient = new Recipient(
            name,
            new List<string> { req.Email },
            req.ResponsibleComponents,
            board,
            req.GroupingStrategy
        );
        recipients.Add(newRecipient);
        _recipientStorage.SaveRecipients(recipients);
        return Ok();
    }

    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        return Redirect("/RecipientsDashboard");
    }

    public class UpdateRecipientRequest
    {
        public string Email { get; set; }
        public GroupingStrategyType GroupingStrategy { get; set; }
        public List<string> ResponsibleComponents { get; set; }
    }

    [HttpPost("update-recipient")]
    public IActionResult UpdateRecipient([FromBody] UpdateRecipientRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) ||
            req.ResponsibleComponents == null || req.ResponsibleComponents.Count == 0)
            return BadRequest("Email, grouping strategy en componenten zijn verplicht.");

        var recipients = _recipientStorage.LoadRecipients();
        var recipient = recipients.FirstOrDefault(r => r.Emails.Any(e => string.Equals(e, req.Email, StringComparison.OrdinalIgnoreCase)));
        if (recipient == null)
            return NotFound("Recipient met dit e-mailadres niet gevonden.");

        recipient.GroupingStrategy = req.GroupingStrategy;
        recipient.ResponsibleComponents = req.ResponsibleComponents;
        _recipientStorage.SaveRecipients(recipients);
        return Ok();
    }

    [HttpGet("grouping-info")]
    public IActionResult GroupingInfo()
    {
        // Verwijs door naar de Razor Page route
        return Redirect("/GroupingStrategiesInfo");
    }

    [HttpGet("components-by-email")]
    public ActionResult<List<string>> GetComponentsByEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email is verplicht.");

        var recipients = _recipientStorage.LoadRecipients();
        var recipient = recipients.FirstOrDefault(r => r.Emails.Any(e => string.Equals(e, email, StringComparison.OrdinalIgnoreCase)));
        if (recipient == null)
            return NotFound("Recipient met dit e-mailadres niet gevonden.");

        return recipient.ResponsibleComponents ?? new List<string>();
    }
}