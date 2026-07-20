namespace Druware.Server.Email;

/// <summary>
/// Identifies a recipient of an email message.
/// </summary>
/// <param name="Address">The recipient's email address.</param>
/// <param name="DisplayName">An optional display name for the recipient.</param>
public sealed record EmailRecipient(string Address, string? DisplayName = null);
