namespace PaymentGateway.Api.Exceptions
{
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
