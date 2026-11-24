using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.UnitTests.Services;

public class PaymentsRepositoryTests
{
    private readonly PaymentsRepository _repository = new();

    [Fact]
    public void CanStoreAndRetrievePayment()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid()
        };

        // Act
        _repository.Add(payment);
        var result = _repository.Get(payment.Id);

        // Assert
        Assert.Equal(payment, result);
    }

    [Fact]
    public void ReturnsNullWhenPaymentDoesNotExist()
    {
        // Act
        var result = _repository.Get(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CannotAddDuplicatePaymentId()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid()
        };

        _repository.Add(payment);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => _repository.Add(payment));
    }

    [Fact]
    public void CanStorePaymentsConcurrently()
    {
        // Arrange
        var payments = Enumerable.Range(0, 1000)
            .Select(_ => new PostPaymentResponse
            {
                Id = Guid.NewGuid()
            })
            .ToList();

        // Act
        Parallel.ForEach(payments, payment =>
        {
            _repository.Add(payment);
        });

        // Assert
        Assert.All(payments, payment =>
        {
            Assert.Equal(payment, _repository.Get(payment.Id));
        });
    }
}