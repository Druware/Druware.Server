namespace Druware.Server.Email;

/// <summary>
/// Describes an email independently of the service used to deliver it.
/// </summary>
public sealed class EmailMessage
{
    public ICollection<EmailRecipient> To { get; init; } = new List<EmailRecipient>();
    public ICollection<EmailRecipient> Cc { get; init; } = new List<EmailRecipient>();
    public ICollection<EmailRecipient> Bcc { get; init; } = new List<EmailRecipient>();
    public ICollection<EmailRecipient> ReplyTo { get; init; } = new List<EmailRecipient>();
    public string Subject { get; init; } = string.Empty;
    public string? HtmlBody { get; init; }
    public string? PlainTextBody { get; init; }
}
