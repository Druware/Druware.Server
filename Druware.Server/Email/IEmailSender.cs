namespace Druware.Server.Email;

/// <summary>
/// Sends application email through the configured delivery service.
/// </summary>
public interface IEmailSender
{
    bool IsConfigured { get; }

    Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default);
}
