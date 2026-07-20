using Azure;
using Azure.Communication.Email;
using AzureEmailMessage = Azure.Communication.Email.EmailMessage;

namespace Druware.Server.Email;

internal interface IAzureCommunicationEmailClient
{
    Task<string?> SendAsync(
        AzureEmailMessage message,
        CancellationToken cancellationToken);
}

internal sealed class AzureCommunicationEmailClientAdapter : IAzureCommunicationEmailClient
{
    private readonly EmailClient _client;

    public AzureCommunicationEmailClientAdapter(string connectionString)
    {
        _client = new EmailClient(connectionString);
    }

    public async Task<string?> SendAsync(
        AzureEmailMessage message,
        CancellationToken cancellationToken)
    {
        var operation = await _client.SendAsync(
            WaitUntil.Completed,
            message,
            cancellationToken);

        return operation.Id;
    }
}
