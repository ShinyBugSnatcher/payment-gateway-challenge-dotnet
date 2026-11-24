using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Enums;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.UnitTests.Controllers;

public class PaymentsControllerTests
{
    [Theory]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Declined)]
    public async Task RetrievesAPaymentSuccessfully(
    PaymentStatus status)
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = status,
            ExpiryYear = DateTime.UtcNow.Year + 2,
            ExpiryMonth = 12,
            Amount = 1000,
            CardNumberLastFour = 4321,
            Currency = "GBP"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        using var webApplicationFactory =
            new WebApplicationFactory<PaymentsController>();

        using var factory = webApplicationFactory
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<PaymentsRepository>();
                    services.AddSingleton(paymentsRepository);
                }));

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            $"/api/Payments/{payment.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paymentResponse = await response.Content
            .ReadFromJsonAsync<PostPaymentResponse>();

        Assert.NotNull(paymentResponse);

        Assert.Equal(payment.Id, paymentResponse.Id);
        Assert.Equal(payment.Status, paymentResponse.Status);
        Assert.Equal(payment.Amount, paymentResponse.Amount);
        Assert.Equal(payment.Currency, paymentResponse.Currency);
        Assert.Equal(payment.ExpiryMonth, paymentResponse.ExpiryMonth);
        Assert.Equal(payment.ExpiryYear, paymentResponse.ExpiryYear);
        Assert.Equal(
            payment.CardNumberLastFour,
            paymentResponse.CardNumberLastFour);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        using var webApplicationFactory =
            new WebApplicationFactory<PaymentsController>();

        using var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync(
            $"/api/Payments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Returns404IfPaymentIdIsInvalid()
    {
        // Arrange
        using var factory =
            new WebApplicationFactory<PaymentsController>();

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/Payments/invalid-guid");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}