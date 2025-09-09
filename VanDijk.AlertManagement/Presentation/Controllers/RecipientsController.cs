using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for managing recipients and their grouping strategies.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RecipientsController : ControllerBase
{
    private readonly RecipientStorage recipientStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecipientsController"/> class.
    /// </summary>
    /// <param name="recipientStorage">The storage service for recipients.</param>
    public RecipientsController(RecipientStorage recipientStorage)
    {
        this.recipientStorage = recipientStorage;
    }

    /// <summary>
    /// Gets all recipients with their details, including grouping strategy as a string.
    /// </summary>
    /// <returns>A list of recipient objects.</returns>
    [HttpGet]
    public ActionResult<List<object>> GetAll()
    {
        // Toon groupingStrategy als string in de API response
        var recipients = this.recipientStorage.LoadRecipients();
        var result = recipients.Select(r => new
        {
            name = r.Name,
            emails = r.Emails,
            responsibleComponents = r.ResponsibleComponents,
            board = r.Board,
            groupingStrategy = r.GroupingStrategy.ToString(),
        }).ToList();
        return this.Ok(result);
    }

    /// <summary>
    /// Gets the list of available grouping strategy names.
    /// </summary>
    /// <returns>A list of grouping strategy names as strings.</returns>
    [HttpGet("grouping-strategies")]
    public ActionResult<List<string>> GetGroupingStrategies()
    {
        return System.Enum.GetNames(typeof(GroupingStrategyType)).ToList();
    }

    /// <summary>
    /// Adds a new recipient to the storage.
    /// </summary>
    /// <param name="req">The request containing recipient details.</param>
    /// <returns>An IActionResult indicating the result of the operation.</returns>
    [HttpPost]
    public IActionResult AddRecipient([FromBody] AddRecipientRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName) ||
            string.IsNullOrWhiteSpace(req.Email) || req.ResponsibleComponents == null || req.ResponsibleComponents.Count == 0 ||
            string.IsNullOrWhiteSpace(req.Board))
        {
            return this.BadRequest("All fields are required.");
        }

        var recipients = this.recipientStorage.LoadRecipients();
        var name = $"{req.FirstName} {req.LastName}";

        var board = req.Board.StartsWith("Development\\", StringComparison.OrdinalIgnoreCase)
            ? req.Board
            : $"Development\\{req.Board}";

        var newRecipient = new Recipient(
            name,
            new List<string> { req.Email },
            req.ResponsibleComponents,
            board,
            req.GroupingStrategy);
        recipients.Add(newRecipient);
        this.recipientStorage.SaveRecipients(recipients);
        return this.Ok();
    }

    /// <summary>
    /// Redirects to the Recipients Dashboard page.
    /// </summary>
    /// <returns>A redirect result to the Recipients Dashboard page.</returns>
    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        return this.Redirect("/RecipientsDashboard");
    }

    /// <summary>
    /// Updates the grouping strategy and responsible components for a recipient.
    /// </summary>
    /// <param name="req">The request containing the recipient's email, new grouping strategy, and responsible components.</param>
    /// <returns>An IActionResult indicating the result of the update operation.</returns>
    [HttpPost("update-recipient")]
    public IActionResult UpdateRecipient([FromBody] UpdateRecipientRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) ||
            req.ResponsibleComponents == null || req.ResponsibleComponents.Count == 0)
        {
            return this.BadRequest("Email, grouping strategy en componenten zijn verplicht.");
        }

        var recipients = this.recipientStorage.LoadRecipients();
        var recipient = recipients.FirstOrDefault(r => r.Emails.Any(e => string.Equals(e, req.Email, StringComparison.OrdinalIgnoreCase)));
        if (recipient == null)
        {
            return this.NotFound("Recipient met dit e-mailadres niet gevonden.");
        }

        recipient.GroupingStrategy = req.GroupingStrategy;
        recipient.ResponsibleComponents = req.ResponsibleComponents;
        this.recipientStorage.SaveRecipients(recipients);
        return this.Ok();
    }

    /// <summary>
    /// Redirects to the Grouping Strategies Info page.
    /// </summary>
    /// <returns>A redirect result to the Grouping Strategies Info page.</returns>
    [HttpGet("grouping-info")]
    public IActionResult GroupingInfo()
    {
        // Verwijs door naar de Razor Page route
        return this.Redirect("/GroupingStrategiesInfo");
    }

    /// <summary>
    /// Gets the list of responsible components for a recipient by their email address.
    /// </summary>
    /// <param name="email">The email address of the recipient.</param>
    /// <returns>A list of responsible components associated with the recipient.</returns>
    [HttpGet("components-by-email")]
    public ActionResult<List<string>> GetComponentsByEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return this.BadRequest("Email is verplicht.");
        }

        var recipients = this.recipientStorage.LoadRecipients();
        var recipient = recipients.FirstOrDefault(r => r.Emails.Any(e => string.Equals(e, email, StringComparison.OrdinalIgnoreCase)));
        if (recipient == null)
        {
            return this.NotFound("Recipient met dit e-mailadres niet gevonden.");
        }

        return recipient.ResponsibleComponents ?? new List<string>();
    }

    /// <summary>
    /// Represents a request to update a recipient's details.
    /// </summary>
    public class UpdateRecipientRequest
    {
        /// <summary>
        /// Gets or sets the email address of the recipient.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the grouping strategy for the recipient.
        /// </summary>
        public GroupingStrategyType GroupingStrategy { get; set; }

        /// <summary>
        /// Gets or sets the list of responsible components for the recipient.
        /// </summary>
        public List<string>? ResponsibleComponents { get; set; }
    }

    /// <summary>
    /// Represents a request to add a new recipient.
    /// </summary>
    public class AddRecipientRequest
    {
        /// <summary>
        /// Gets or sets the first name of the recipient.
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name of the recipient.
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Gets or sets the email address of the recipient.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the list of responsible components for the recipient.
        /// </summary>
        public List<string>? ResponsibleComponents { get; set; }

        /// <summary>
        /// Gets or sets the board associated with the recipient.
        /// </summary>
        public string? Board { get; set; }

        /// <summary>
        /// Gets or sets the grouping strategy for the recipient.
        /// </summary>
        public GroupingStrategyType GroupingStrategy { get; set; }
    }
}