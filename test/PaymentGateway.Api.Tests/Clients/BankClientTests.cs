using System.Net;
using System.Text;
using System.Text.Json;

using Moq;
using Moq.Protected;

using PaymentGateway.Api.Clients;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models.Bank;

namespace PaymentGateway.Api.UnitTests.Clients;

public class BankClientTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly BankClient _bankClient;

    public BankClientTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>(
            MockBehavior.Strict);

        // Allow HttpClient to dispose of the mocked handler
        _handlerMock.Protected()
            .Setup("Dispose", ItExpr.IsAny<bool>());

        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        _bankClient = new BankClient(_httpClient);
    }

    [Fact]
    public async Task ReturnsAuthorizedWhenBankAuthorizesPayment()
    {
        // Arrange
        var request = CreateValidBankRequest();

        SetupBankResponse(
            HttpStatusCode.OK,
            """
            {
                "authorized": true,
                "authorization_code": "123456"
            }
            """);

        // Act
        var response = await _bankClient
            .ProcessPaymentAsync(request);

        // Assert
        Assert.True(response.Authorized);
        Assert.Equal("123456", response.AuthorizationCode);
    }

    [Fact]
    public async Task ReturnsDeclinedWhenBankDeclinesPayment()
    {
        // Arrange
        var request = CreateValidBankRequest();

        SetupBankResponse(
            HttpStatusCode.OK,
            """
            {
                "authorized": false,
                "authorization_code": ""
            }
            """);

        // Act
        var response = await _bankClient
            .ProcessPaymentAsync(request);

        // Assert
        Assert.False(response.Authorized);
        Assert.Equal(string.Empty, response.AuthorizationCode);
    }

    [Fact]
    public async Task SendsPostRequestToPaymentsEndpoint()
    {
        // Arrange
        var request = CreateValidBankRequest();

        SetupBankResponse(
            HttpStatusCode.OK,
            """{"authorized": true, "authorization_code": "123456"}""");

        // Act
        await _bankClient.ProcessPaymentAsync(request);

        // Assert
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Post &&
                message.RequestUri ==
                    new Uri("http://localhost:8080/payments")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SerializesRequestUsingCorrectPropertyNames()
    {
        // Arrange
        var request = CreateValidBankRequest();

        string? requestBody = null;

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (
                HttpRequestMessage message,
                CancellationToken cancellationToken) =>
            {
                requestBody = await message.Content!
                    .ReadAsStringAsync(cancellationToken);

                return CreateHttpResponse(
                    HttpStatusCode.OK,
                    """{"authorized": true, "authorization_code": "123456"}""");
            });

        // Act
        await _bankClient.ProcessPaymentAsync(request);

        // Assert
        Assert.NotNull(requestBody);

        using var json = JsonDocument.Parse(requestBody!);

        var root = json.RootElement;

        Assert.Equal(
            request.CardNumber,
            root.GetProperty("card_number").GetString());

        Assert.Equal(
            request.ExpiryDate,
            root.GetProperty("expiry_date").GetString());

        Assert.Equal(
            request.Currency,
            root.GetProperty("currency").GetString());

        Assert.Equal(
            request.Amount,
            root.GetProperty("amount").GetInt32());

        Assert.Equal(
            request.Cvv,
            root.GetProperty("cvv").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ThrowsExceptionWhenBankReturnsError(
    HttpStatusCode statusCode)
    {
        // Arrange
        var request = CreateValidBankRequest();

        SetupBankResponse(statusCode, "{}");

        // Act
        var exception = await Assert
            .ThrowsAsync<BankCommunicationException>(
                () => _bankClient.ProcessPaymentAsync(request));

        // Assert
        var innerException = Assert.IsType<HttpRequestException>(
            exception.InnerException);

        Assert.Equal<HttpStatusCode?>(
            statusCode,
            innerException.StatusCode);
    }

    [Fact]
    public async Task ThrowsExceptionWhenBankReturnsNull()
    {
        // Arrange
        var request = CreateValidBankRequest();

        SetupBankResponse(HttpStatusCode.OK, "null");

        // Act & Assert
        await Assert.ThrowsAsync<BankCommunicationException>(
            () => _bankClient.ProcessPaymentAsync(request));
    }

    [Fact]
    public async Task ThrowsExceptionWhenBankReturnsMalformedJson()
    {
        // Arrange
        var request = CreateValidBankRequest();

        SetupBankResponse(
            HttpStatusCode.OK,
            "Invalid JSON");

        // Act
        var exception = await Assert
            .ThrowsAsync<BankCommunicationException>(
                () => _bankClient.ProcessPaymentAsync(request));

        // Assert
        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public async Task ThrowsCommunicationExceptionWhenBankIsUnreachable()
    {
        // Arrange
        var request = CreateValidBankRequest();

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException(
                "Connection refused."));

        // Act & Assert
        await Assert.ThrowsAsync<BankCommunicationException>(
            () => _bankClient.ProcessPaymentAsync(request));
    }

    [Fact]
    public async Task ThrowsTimeoutExceptionWhenBankTimesOut()
    {
        // Arrange
        var request = CreateValidBankRequest();

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException(
                "Request timed out."));

        // Act & Assert
        await Assert.ThrowsAsync<BankTimeoutException>(
            () => _bankClient.ProcessPaymentAsync(request));
    }

    [Fact]
    public async Task PreservesCallerCancellation()
    {
        // Arrange
        var request = CreateValidBankRequest();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (
                HttpRequestMessage message,
                CancellationToken token) =>
            {
                cancellationTokenSource.Cancel();

                await Task.Delay(
                    Timeout.InfiniteTimeSpan,
                    token);

                return new HttpResponseMessage();
            });

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _bankClient.ProcessPaymentAsync(
                request,
                cancellationTokenSource.Token));
    }

    // Helper methods

    private void SetupBankResponse(
        HttpStatusCode statusCode,
        string responseBody)
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(
                CreateHttpResponse(statusCode, responseBody));
    }

    private static HttpResponseMessage CreateHttpResponse(
        HttpStatusCode statusCode,
        string responseBody)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                responseBody,
                Encoding.UTF8,
                "application/json")
        };
    }

    private static BankPaymentRequest CreateValidBankRequest()
    {
        return new BankPaymentRequest
        {
            CardNumber = "1234567890124321",
            ExpiryDate = $"12/{DateTime.UtcNow.Year + 2}",
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };
    }

    

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}