using PaymentGateway.Api.Models.Bank;

namespace PaymentGateway.Api.Clients.Interfaces;

public interface IBankClient
{
    Task<BankPaymentResponse> ProcessPaymentAsync(
        BankPaymentRequest request,
        CancellationToken cancellationToken = default);
}