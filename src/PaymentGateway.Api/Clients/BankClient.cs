using System.Net.Http.Json;

using PaymentGateway.Api.Clients.Interfaces;
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
        using var response = await _httpClient.PostAsJsonAsync(
            "payments",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<BankPaymentResponse>(
                cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException(
                "Invalid response from acquiring bank.");
    }
}