namespace PaymentGateway.Api.Exceptions;

public class BankCommunicationException : Exception
{
    public BankCommunicationException(
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
    }
}