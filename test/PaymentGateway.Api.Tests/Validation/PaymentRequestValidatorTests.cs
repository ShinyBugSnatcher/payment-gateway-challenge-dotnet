using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validation;

using Xunit;

namespace PaymentGateway.Api.UnitTests.PaymentRequestValidatorTests;

public class PaymentRequestValidatorTests
{
    private readonly PaymentRequestValidationHelper _validator = new();

    private static PostPaymentRequest CreateValidRequest()
    {
        return new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };
    }

    #region Valid Request

    [Fact]
    public void Validate_ValidRequest_ReturnsNoErrors()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Empty(errors);
    }

    #endregion

    #region Card Number

    [Theory]
    [InlineData("12345678901234")]
    [InlineData("1234567890123456")]
    [InlineData("1234567890123456789")]
    public void Validate_ValidCardNumber_ReturnsNoErrors(
        string cardNumber)
    {
        // Arrange
        var request = CreateValidRequest();
        request.CardNumber = cardNumber;

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("1234567890123")]
    [InlineData("12345678901234567890")]
    [InlineData("123456789012345A")]
    [InlineData("1234 5678 9012 3456")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_InvalidCardNumber_ReturnsError(
        string? cardNumber)
    {
        // Arrange
        var request = CreateValidRequest();
        request.CardNumber = cardNumber!;

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Invalid card number.", errors);
    }

    #endregion

    #region Expiry Month

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public void Validate_ValidExpiryMonth_ReturnsNoErrors(
        int expiryMonth)
    {
        // Arrange
        var request = CreateValidRequest();
        request.ExpiryMonth = expiryMonth;

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(13)]
    public void Validate_InvalidExpiryMonth_ReturnsError(
        int expiryMonth)
    {
        // Arrange
        var request = CreateValidRequest();
        request.ExpiryMonth = expiryMonth;

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Invalid expiry month.", errors);
    }

    #endregion

    #region Expiry Date

    [Fact]
    public void Validate_PreviousExpiryMonth_ReturnsError()
    {
        // Arrange
        var request = CreateValidRequest();
        var expiredDate = DateTime.UtcNow.AddMonths(-1);

        request.ExpiryMonth = expiredDate.Month;
        request.ExpiryYear = expiredDate.Year;

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Card has expired.", errors);
    }

    [Fact]
    public void Validate_PreviousExpiryYear_ReturnsError()
    {
        // Arrange
        var request = CreateValidRequest();
        request.ExpiryYear = DateTime.UtcNow.Year - 1;

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Card has expired.", errors);
    }

    [Fact]
    public void Validate_CurrentExpiryMonth_ReturnsNoErrors()
    {
        // Arrange
        var request = CreateValidRequest();
        var now = DateTime.UtcNow;

        request.ExpiryMonth = now.Month;
        request.ExpiryYear = now.Year;

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_FutureExpiryDate_ReturnsNoErrors()
    {
        // Arrange
        var request = CreateValidRequest();
        var futureDate = DateTime.UtcNow.AddMonths(1);

        request.ExpiryMonth = futureDate.Month;
        request.ExpiryYear = futureDate.Year;

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Empty(errors);
    }

    #endregion

    #region Currency

    [Theory]
    [InlineData("GBP")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public void Validate_SupportedCurrency_ReturnsNoErrors(
        string currency)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Currency = currency;

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("AUD")]
    [InlineData("JPY")]
    [InlineData("gbp")]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_UnsupportedCurrency_ReturnsError(
        string? currency)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Currency = currency!;

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Unsupported currency.", errors);
    }

    #endregion

    #region Amount

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void Validate_ValidAmount_ReturnsNoErrors(
        int amount)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Amount = amount;

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Validate_InvalidAmount_ReturnsError(
        int amount)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Amount = amount;

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains(
            "Amount must be greater than zero.", errors);
    }

    #endregion

    #region CVV

    [Theory]
    [InlineData("123")]
    [InlineData("1234")]
    [InlineData("012")]
    [InlineData("0123")]
    public void Validate_ValidCvv_ReturnsNoErrors(
        string cvv)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Cvv = cvv;

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("12A")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_InvalidCvv_ReturnsError(
        string? cvv)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Cvv = cvv!;

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Invalid CVV.", errors);
    }

    #endregion

    #region Multiple Validation Errors

    [Fact]
    public void Validate_MultipleInvalidFields_ReturnsAllErrors()
    {
        // Arrange
        var request = CreateValidRequest();

        request.CardNumber = "123";
        request.ExpiryMonth = 13;
        request.Currency = "AUD";
        request.Amount = -100;
        request.Cvv = "1";

        // Act
        var errors = _validator.Validate(request);

        // Assert
        Assert.Equal(5, errors.Count);

        Assert.Contains("Invalid card number.", errors);
        Assert.Contains("Invalid expiry month.", errors);
        Assert.Contains("Unsupported currency.", errors);
        Assert.Contains(
            "Amount must be greater than zero.", errors);
        Assert.Contains("Invalid CVV.", errors);
    }

    #endregion
}