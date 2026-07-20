using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Druware.Server.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Azure Communication Services email sender.
    /// </summary>
    public static IServiceCollection AddDruwareAzureEmail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IEmailSender>(
            _ => new AzureCommunicationEmailSender(configuration));
        return services;
    }
}
