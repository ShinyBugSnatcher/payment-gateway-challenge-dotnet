using System.Net.Http.Json;
using System.Text.Json;

using PaymentGateway.Api.Clients.Interfaces;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models.Bank;

namespace PaymentGateway.Api.Clients;

public class BankClient : IBankClient
{
    private readonly HttpClient _httpClient;

    public BankClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BankPaymentResponse> ProcessPaymentAsync(
        BankPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "payments",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<BankPaymentResponse>(
                    cancellationToken: cancellationToken)
                ?? throw new BankCommunicationException(
                    "Empty response from acquiring bank.");
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new BankTimeoutException(
                "Acquiring bank request timed out.",
                exception);
        }
        catch (HttpRequestException exception)
        {
            throw new BankCommunicationException(
                "Failed to communicate with acquiring bank.",
                exception);
        }
        catch (JsonException exception)
        {
            throw new BankCommunicationException(
                "Invalid response from acquiring bank.",
                exception);
        }
    }
}