using System.Timers;

using Moq;

using PaymentGateway.Api.Clients;
using PaymentGateway.Api.Clients.Interfaces;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models.Bank;
using PaymentGateway.Api.Models.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

using Xunit;

using static PaymentGateway.Api.Validation.PaymentRequestValidationHelper;

namespace PaymentGateway.Api.UnitTests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IBankClient> _bankClientMock;
    private readonly PaymentsRepository _paymentsRepository;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _bankClientMock = new Mock<IBankClient>();
        _paymentsRepository = new PaymentsRepository();

        _paymentService = new PaymentService(
            _paymentsRepository,
            _bankClientMock.Object);
    }

    [Fact]
    public async Task ReturnsAuthorizedWhenBankAuthorizesPayment()
    {
        // Arrange
        var request = CreateValidPaymentRequest();

        SetupBankResponse(true);

        // Act
        var response = await _paymentService
            .ProcessPaymentAsync(request);

        // Assert
        Assert.Equal(PaymentStatus.Authorized, response.Status);
    }

    [Fact]
    public async Task ReturnsDeclinedWhenBankDeclinesPayment()
    {
        // Arrange
        var request = CreateValidPaymentRequest();

        SetupBankResponse(false);

        // Act
        var response = await _paymentService
            .ProcessPaymentAsync(request);

        // Assert
        Assert.Equal(PaymentStatus.Declined, response.Status);
    }

    [Fact]
    public async Task DoesNotContactBankWhenPaymentIsInvalid()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.Amount = -100;

        // Act
        var exception = await Assert
            .ThrowsAsync<PaymentValidationException>(
                () => _paymentService.ProcessPaymentAsync(request));

        // Assert
        Assert.Contains(
            "Amount must be greater than zero.",
            exception.Errors);

        _bankClientMock.Verify(
            client => client.ProcessPaymentAsync(
                It.IsAny<BankPaymentRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReturnsAllValidationErrorsWhenPaymentIsInvalid()
    {
        // Arrange
        var request = CreateValidPaymentRequest();

        request.Amount = -100;
        request.CardNumber = "123";
        request.Cvv = "1";

        // Act
        var exception = await Assert
            .ThrowsAsync<PaymentValidationException>(
                () => _paymentService.ProcessPaymentAsync(request));

        // Assert
        Assert.Contains(
            "Amount must be greater than zero.",
            exception.Errors);

        Assert.Contains(
            "Invalid card number.",
            exception.Errors);

        Assert.Contains(
            "Invalid CVV.",
            exception.Errors);

        Assert.Equal(3, exception.Errors.Count);

        _bankClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ContactsBankOnceWhenPaymentIsValid()
    {
        // Arrange
        var request = CreateValidPaymentRequest();

        SetupBankResponse(true);

        // Act
        await _paymentService.ProcessPaymentAsync(request);

        // Assert
        _bankClientMock.Verify(
            client => client.ProcessPaymentAsync(
                It.IsAny<BankPaymentRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MapsPaymentRequestToBankRequest()
    {
        // Arrange
        var request = CreateValidPaymentRequest();

        SetupBankResponse(true);

        // Act
        await _paymentService.ProcessPaymentAsync(request);

        // Assert
        _bankClientMock.Verify(
            client => client.ProcessPaymentAsync(
                It.Is<BankPaymentRequest>(bankRequest =>
                    bankRequest.CardNumber == request.CardNumber &&
                    bankRequest.ExpiryDate ==
                        $"{request.ExpiryMonth:D2}/{request.ExpiryYear:D4}" &&
                    bankRequest.Currency == request.Currency &&
                    bankRequest.Amount == request.Amount &&
                    bankRequest.Cvv == request.Cvv),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MapsPaymentDetailsToResponse()
    {
        // Arrange
        var request = CreateValidPaymentRequest();

        SetupBankResponse(true);

        // Act
        var response = await _paymentService
            .ProcessPaymentAsync(request);

        // Assert
        Assert.Equal(4321, response.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, response.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, response.ExpiryYear);
        Assert.Equal(request.Currency, response.Currency);
        Assert.Equal(request.Amount, response.Amount);
    }

    [Fact]
    public async Task GeneratesPaymentId()
    {
        // Arrange
        var request = CreateValidPaymentRequest();

        SetupBankResponse(true);

        // Act
        var response = await _paymentService
            .ProcessPaymentAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, response.Id);
    }

    private void SetupBankResponse(bool authorized)
    {
        _bankClientMock
            .Setup(client => client.ProcessPaymentAsync(
                It.IsAny<BankPaymentRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankPaymentResponse
            {
                Authorized = authorized,
                AuthorizationCode = authorized
                    ? "123456"
                    : null
            });
    }

    private static PostPaymentRequest CreateValidPaymentRequest()
    {
        return new PostPaymentRequest
        {
            CardNumber = "1234567890124321",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 2,
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };
    }
}