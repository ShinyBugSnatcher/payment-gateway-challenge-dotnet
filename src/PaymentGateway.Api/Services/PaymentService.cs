using PaymentGateway.Api.Clients.Interfaces;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models.Bank;
using PaymentGateway.Api.Models.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Validation;

using static PaymentGateway.Api.Validation.PaymentRequestValidationHelper;

namespace PaymentGateway.Api.Services;

public class PaymentService
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly IBankClient _bankClient;

    private readonly PaymentRequestValidationHelper _validator = new();

    public PaymentService(
        PaymentsRepository paymentsRepository,
        IBankClient bankClient)
    {
        _paymentsRepository = paymentsRepository;
        _bankClient = bankClient;
    }

    public async Task<PostPaymentResponse> ProcessPaymentAsync(
        PostPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate payment request
        var validationErrors = _validator.Validate(request);

        if (validationErrors.Count > 0)
        {
            throw new PaymentValidationException(validationErrors);
        }

        // Map payment request to bank request
        var bankRequest = new BankPaymentRequest
        {
            CardNumber = request.CardNumber,
            ExpiryDate = $"{request.ExpiryMonth:D2}/{request.ExpiryYear:D4}",
            Currency = request.Currency,
            Amount = request.Amount,
            Cvv = request.Cvv
        };

        // Send payment to bank
        var bankResponse = await _bankClient.ProcessPaymentAsync(
            bankRequest,
            cancellationToken);

        // Map bank response to payment response
        var paymentResponse = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = bankResponse.Authorized
                ? PaymentStatus.Authorized
                : PaymentStatus.Declined,
            CardNumberLastFour = int.Parse(request.CardNumber[^4..]),
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency,
            Amount = request.Amount
        };

        // Persist processed payment
        _paymentsRepository.Add(paymentResponse);

        return paymentResponse;
    }
}