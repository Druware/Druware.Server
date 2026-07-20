using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using AzureEmailMessage = Azure.Communication.Email.EmailMessage;

namespace Druware.Server.Email;

/// <summary>
/// Sends email through Azure Communication Services Email.
/// </summary>
public sealed class AzureCommunicationEmailSender : IEmailSender
{
    public const string ConnectionStringEnvironmentKey =
        "COMMUNICATION_SERVICES_CONNECTION_STRING";
    public const string ConnectionStringKey = "API:Mail:Azure:ConnectionString";
    public const string SenderAddressKey = "API:Mail:Azure:SenderAddress";
    public const string LegacySenderAddressKey = "API:Notification:From";

    private readonly IAzureCommunicationEmailClient? _client;
    private readonly string? _senderAddress;

    public AzureCommunicationEmailSender(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _senderAddress = ResolveSenderAddress(configuration);
        var connectionString = ResolveConnectionString(configuration);
        if (!string.IsNullOrWhiteSpace(connectionString))
            _client = new AzureCommunicationEmailClientAdapter(connectionString);
    }

    internal AzureCommunicationEmailSender(
        IConfiguration configuration,
        IAzureCommunicationEmailClient client)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(client);

        _senderAddress = ResolveSenderAddress(configuration);
        _client = client;
    }

    /// <summary>
    /// Indicates whether both an Azure client and sender address are configured.
    /// </summary>
    public bool IsConfigured =>
        _client != null && !string.IsNullOrWhiteSpace(_senderAddress);

    public async Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        Validate(message);

        if (string.IsNullOrWhiteSpace(_senderAddress))
        {
            throw new InvalidOperationException(
                $"Azure email sender address is not configured. Set '{SenderAddressKey}'.");
        }

        if (_client == null)
        {
            throw new InvalidOperationException(
                "Azure Communication Services email is not configured. Set the " +
                $"'{ConnectionStringEnvironmentKey}' environment variable or " +
                $"'{ConnectionStringKey}' configuration value.");
        }

        var content = new EmailContent(message.Subject)
        {
            Html = message.HtmlBody,
            PlainText = message.PlainTextBody
        };

        var recipients = new EmailRecipients(
            message.To.Select(ToAzureAddress),
            message.Cc.Select(ToAzureAddress),
            message.Bcc.Select(ToAzureAddress));

        var azureMessage = new AzureEmailMessage(
            _senderAddress,
            recipients,
            content);

        foreach (var replyTo in message.ReplyTo)
            azureMessage.ReplyTo.Add(ToAzureAddress(replyTo));

        try
        {
            var operationId = await _client.SendAsync(
                azureMessage,
                cancellationToken);
            return EmailSendResult.Success(operationId);
        }
        catch (RequestFailedException exception)
        {
            return EmailSendResult.Failure(
                exception.Status,
                exception.ErrorCode,
                exception.Message);
        }
    }

    internal static string? ResolveConnectionString(IConfiguration configuration) =>
        FirstNonBlank(
            Environment.GetEnvironmentVariable(ConnectionStringEnvironmentKey),
            configuration[ConnectionStringKey]);

    internal static string? ResolveSenderAddress(IConfiguration configuration) =>
        FirstNonBlank(
            configuration[SenderAddressKey],
            configuration[LegacySenderAddressKey]);

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static EmailAddress ToAzureAddress(EmailRecipient recipient) =>
        new(recipient.Address, recipient.DisplayName);

    private static void Validate(EmailMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.To == null || message.To.Count == 0)
            throw new ArgumentException("At least one To recipient is required.", nameof(message));

        if (message.Cc == null || message.Bcc == null || message.ReplyTo == null)
        {
            throw new ArgumentException(
                "Recipient collections cannot be null.",
                nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.Subject))
            throw new ArgumentException("An email subject is required.", nameof(message));

        if (string.IsNullOrWhiteSpace(message.HtmlBody)
            && string.IsNullOrWhiteSpace(message.PlainTextBody))
        {
            throw new ArgumentException(
                "An HTML or plain-text email body is required.",
                nameof(message));
        }

        ValidateRecipients(message.To, nameof(message.To));
        ValidateRecipients(message.Cc, nameof(message.Cc));
        ValidateRecipients(message.Bcc, nameof(message.Bcc));
        ValidateRecipients(message.ReplyTo, nameof(message.ReplyTo));
    }

    private static void ValidateRecipients(
        IEnumerable<EmailRecipient> recipients,
        string parameterName)
    {
        if (recipients.Any(recipient =>
                recipient == null || string.IsNullOrWhiteSpace(recipient.Address)))
        {
            throw new ArgumentException(
                "Recipient email addresses cannot be empty.",
                parameterName);
        }
    }
}
