/// <summary>
/// Specifies the available alert channel types for sending alerts.
/// </summary>
public enum AlertChannelType
{
    /// <summary>
    /// Alert will be sent via Email.
    /// </summary>
    Email,
    /// <summary>
    /// Alert will be sent via Microsoft Teams.
    /// </summary>
    Teams,
    /// <summary>
    /// Alert will be sent via both Email and Teams.
    /// </summary>
    Both,
    /// <summary>
    /// Alert will be sent via SMTP.
    /// </summary>
    Smtp,
}