using PaymentGateway.Api.Models.Enums;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Validation
{
    public class PaymentRequestValidationHelper
    {
        public List<string> Validate(PostPaymentRequest request)
        {
            var errors = new List<string>();

            // Validate card number
            if (string.IsNullOrWhiteSpace(request.CardNumber) ||
                request.CardNumber.Length is < 14 or > 19 ||
                !request.CardNumber.All(char.IsAsciiDigit))
            {
                errors.Add("Invalid card number.");
            }

            // Validate expiry date
            var now = DateTime.UtcNow;

            if (request.ExpiryMonth is < 1 or > 12)
            {
                errors.Add("Invalid expiry month.");
            }
            else if (request.ExpiryYear < now.Year ||
                    (request.ExpiryYear == now.Year &&
                     request.ExpiryMonth < now.Month))
            {
                errors.Add("Card has expired.");
            }

            // Validate currency
            if (!Enum.GetNames<SupportedCurrency>().Contains(request.Currency))
            {
                errors.Add("Unsupported currency.");
            }

            // Validate amount
            if (request.Amount <= 0)
            {
                errors.Add("Amount must be greater than zero.");
            }

            // Validate CVV
            if (string.IsNullOrWhiteSpace(request.Cvv) ||
                request.Cvv.Length is < 3 or > 4 ||
                !request.Cvv.All(char.IsAsciiDigit))
            {
                errors.Add("Invalid CVV.");
            }

            return errors;
        }

        public class PaymentValidationException : Exception
        {
            public IReadOnlyList<string> Errors { get; }

            public PaymentValidationException(
                IEnumerable<string> errors)
                : base("Payment request validation failed.")
            {
                Errors = errors.ToList().AsReadOnly();
            }
        }
    }
}
