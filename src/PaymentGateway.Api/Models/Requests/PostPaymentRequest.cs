namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest
{
    public string CardNumber { get; set; } = string.Empty; //Changed to string as is an identifier, not a numerical value
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Currency { get; set; } = string.Empty; //Initialising strings here to avoid null references
    public int Amount { get; set; }
    public string Cvv { get; set; } = string.Empty; //Changed to string as to preserve full number (including leading 0s)
}