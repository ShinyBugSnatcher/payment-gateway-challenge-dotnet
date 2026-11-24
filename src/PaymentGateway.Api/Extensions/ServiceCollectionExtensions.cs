using Microsoft.Extensions.Options;

using PaymentGateway.Api.Clients;
using PaymentGateway.Api.Clients.Interfaces;
using PaymentGateway.Api.Configuration;

namespace PaymentGateway.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBankClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BankSettings>()
            .Bind(configuration.GetSection(BankSettings.SectionName))
            .Validate(settings =>
                Uri.TryCreate(
                    settings.BaseUrl,
                    UriKind.Absolute,
                    out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp
                    || uri.Scheme == Uri.UriSchemeHttps),
                "Bank BaseUrl must be a valid HTTP or HTTPS URL.")
            .Validate(settings => settings.TimeoutSeconds > 0,
                "Bank timeout must be greater than zero.")
            .ValidateOnStart();

        services.AddHttpClient<IBankClient, BankClient>(
            (serviceProvider, client) =>
            {
                var settings = serviceProvider
                    .GetRequiredService<IOptions<BankSettings>>()
                    .Value;

                client.BaseAddress = new Uri(settings.BaseUrl);

                client.Timeout = TimeSpan.FromSeconds(
                    settings.TimeoutSeconds);
            });

        return services;
    }
}