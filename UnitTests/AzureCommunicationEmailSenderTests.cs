using Azure;
using Azure.Communication.Email;
using Druware.Server.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AzureEmailMessage = Azure.Communication.Email.EmailMessage;
using DruwareEmailMessage = Druware.Server.Email.EmailMessage;

namespace UnitTests;

[TestFixture]
public class AzureCommunicationEmailSenderTests
{
    [Test]
    public async Task SendAsync_MapsMessageAndReturnsOperationId()
    {
        var client = new RecordingEmailClient { OperationId = "operation-123" };
        var sender = CreateSender(client);
        using var cancellationSource = new CancellationTokenSource();
        var message = new DruwareEmailMessage
        {
            To = { new EmailRecipient("to@example.com", "To Person") },
            Cc = { new EmailRecipient("cc@example.com") },
            Bcc = { new EmailRecipient("bcc@example.com") },
            ReplyTo = { new EmailRecipient("reply@example.com", "Support") },
            Subject = "Message subject",
            HtmlBody = "<strong>HTML body</strong>",
            PlainTextBody = "Plain body"
        };

        var result = await sender.SendAsync(message, cancellationSource.Token);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(sender.IsConfigured, Is.True);
            Assert.That(result.OperationId, Is.EqualTo("operation-123"));
            Assert.That(client.CancellationToken, Is.EqualTo(cancellationSource.Token));
            Assert.That(client.Message, Is.Not.Null);
            Assert.That(client.Message!.SenderAddress, Is.EqualTo("sender@example.com"));
            Assert.That(client.Message.Content.Subject, Is.EqualTo("Message subject"));
            Assert.That(client.Message.Content.Html, Is.EqualTo("<strong>HTML body</strong>"));
            Assert.That(client.Message.Content.PlainText, Is.EqualTo("Plain body"));
            Assert.That(client.Message.Recipients.To.Single().Address, Is.EqualTo("to@example.com"));
            Assert.That(client.Message.Recipients.To.Single().DisplayName, Is.EqualTo("To Person"));
            Assert.That(client.Message.Recipients.CC.Single().Address, Is.EqualTo("cc@example.com"));
            Assert.That(client.Message.Recipients.BCC.Single().Address, Is.EqualTo("bcc@example.com"));
            Assert.That(client.Message.ReplyTo.Single().Address, Is.EqualTo("reply@example.com"));
            Assert.That(client.Message.ReplyTo.Single().DisplayName, Is.EqualTo("Support"));
        });
    }

    [Test]
    public async Task SendAsync_ReturnsStructuredFailureForAzureRequestFailure()
    {
        var client = new RecordingEmailClient
        {
            Exception = new RequestFailedException(
                429,
                "Rate limited",
                "TooManyRequests",
                null)
        };
        var sender = CreateSender(client);

        var result = await sender.SendAsync(ValidMessage());

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.OperationId, Is.Null);
            Assert.That(result.Status, Is.EqualTo(429));
            Assert.That(result.ErrorCode, Is.EqualTo("TooManyRequests"));
            Assert.That(result.ErrorMessage, Does.Contain("Rate limited"));
        });
    }

    [Test]
    public void SendAsync_PropagatesCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var client = new RecordingEmailClient
        {
            Exception = new OperationCanceledException(cancellationSource.Token)
        };
        var sender = CreateSender(client);

        Assert.That(
            async () => await sender.SendAsync(ValidMessage(), cancellationSource.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [TestCase("missing-to")]
    [TestCase("missing-subject")]
    [TestCase("missing-body")]
    [TestCase("empty-address")]
    public void SendAsync_ThrowsForInvalidMessages(string scenario)
    {
        var message = ValidMessage();
        switch (scenario)
        {
            case "missing-to":
                message.To.Clear();
                break;
            case "missing-subject":
                message = Copy(message, subject: " ");
                break;
            case "missing-body":
                message = Copy(message, plainTextBody: null);
                break;
            case "empty-address":
                message.To.Clear();
                message.To.Add(new EmailRecipient(" "));
                break;
        }

        var sender = CreateSender(new RecordingEmailClient());

        Assert.That(
            async () => await sender.SendAsync(message),
            Throws.ArgumentException);
    }

    [Test]
    public async Task SendAsync_UsesLegacyNotificationSenderAsFallback()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            [AzureCommunicationEmailSender.LegacySenderAddressKey] = "legacy@example.com"
        });
        var client = new RecordingEmailClient();
        var sender = new AzureCommunicationEmailSender(configuration, client);

        await sender.SendAsync(ValidMessage());

        Assert.That(client.Message!.SenderAddress, Is.EqualTo("legacy@example.com"));
    }

    [Test]
    [NonParallelizable]
    public void ResolveConnectionString_PrefersStandardEnvironmentVariable()
    {
        const string environmentValue = "endpoint=https://environment.example;accesskey=environment";
        var originalValue = Environment.GetEnvironmentVariable(
            AzureCommunicationEmailSender.ConnectionStringEnvironmentKey);
        var configuration = Configuration(new Dictionary<string, string?>
        {
            [AzureCommunicationEmailSender.ConnectionStringKey] =
                "endpoint=https://configuration.example;accesskey=configuration"
        });

        try
        {
            Environment.SetEnvironmentVariable(
                AzureCommunicationEmailSender.ConnectionStringEnvironmentKey,
                environmentValue);

            var result = AzureCommunicationEmailSender.ResolveConnectionString(configuration);

            Assert.That(result, Is.EqualTo(environmentValue));
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                AzureCommunicationEmailSender.ConnectionStringEnvironmentKey,
                originalValue);
        }
    }

    [Test]
    [NonParallelizable]
    public void ResolveConnectionString_UsesConfigurationWhenEnvironmentIsEmpty()
    {
        const string configurationValue =
            "endpoint=https://configuration.example;accesskey=configuration";
        var originalValue = Environment.GetEnvironmentVariable(
            AzureCommunicationEmailSender.ConnectionStringEnvironmentKey);
        var configuration = Configuration(new Dictionary<string, string?>
        {
            [AzureCommunicationEmailSender.ConnectionStringKey] = configurationValue
        });

        try
        {
            Environment.SetEnvironmentVariable(
                AzureCommunicationEmailSender.ConnectionStringEnvironmentKey,
                string.Empty);

            var result = AzureCommunicationEmailSender.ResolveConnectionString(configuration);

            Assert.That(result, Is.EqualTo(configurationValue));
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                AzureCommunicationEmailSender.ConnectionStringEnvironmentKey,
                originalValue);
        }
    }

    [Test]
    public void AddDruwareAzureEmail_RegistersSingletonEmailSender()
    {
        var services = new ServiceCollection();
        services.AddDruwareAzureEmail(Configuration());
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IEmailSender>();
        var second = provider.GetRequiredService<IEmailSender>();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.TypeOf<AzureCommunicationEmailSender>());
            Assert.That(second, Is.SameAs(first));
        });
    }

    private static AzureCommunicationEmailSender CreateSender(
        IAzureCommunicationEmailClient client) =>
        new(Configuration(new Dictionary<string, string?>
        {
            [AzureCommunicationEmailSender.SenderAddressKey] = "sender@example.com"
        }), client);

    private static DruwareEmailMessage ValidMessage() => new()
    {
        To = { new EmailRecipient("recipient@example.com") },
        Subject = "Subject",
        PlainTextBody = "Body"
    };

    private static DruwareEmailMessage Copy(
        DruwareEmailMessage message,
        string? subject = null,
        string? plainTextBody = "Body") => new()
    {
        To = new List<EmailRecipient>(message.To),
        Subject = subject ?? message.Subject,
        HtmlBody = message.HtmlBody,
        PlainTextBody = plainTextBody
    };

    private static IConfiguration Configuration(
        IDictionary<string, string?>? values = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
            .Build();

    private sealed class RecordingEmailClient : IAzureCommunicationEmailClient
    {
        public AzureEmailMessage? Message { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public string? OperationId { get; init; }
        public Exception? Exception { get; init; }

        public Task<string?> SendAsync(
            AzureEmailMessage message,
            CancellationToken cancellationToken)
        {
            Message = message;
            CancellationToken = cancellationToken;

            if (Exception != null)
                return Task.FromException<string?>(Exception);

            return Task.FromResult(OperationId);
        }
    }
}
