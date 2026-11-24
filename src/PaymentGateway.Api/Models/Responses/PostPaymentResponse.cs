using PaymentGateway.Api.Models.Enums;

namespace PaymentGateway.Api.Models.Responses;

public class PostPaymentResponse
{
    public Guid Id { get; set; }
    public PaymentStatus Status { get; set; }
    public int CardNumberLastFour { get; set; }
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Currency { get; set; }
    public int Amount { get; set; }
}

public class RejectedPaymentResponse
{
    public PaymentStatus Status { get; init; }
        = PaymentStatus.Rejected;

    public IReadOnlyList<string> Errors { get; init; }
        = Array.Empty<string>();
}
