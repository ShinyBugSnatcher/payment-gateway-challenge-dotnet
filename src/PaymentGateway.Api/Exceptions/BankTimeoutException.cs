namespace PaymentGateway.Api.Exceptions
{
    public class BankTimeoutException : Exception
    {
        public BankTimeoutException(
            string message,
            Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
