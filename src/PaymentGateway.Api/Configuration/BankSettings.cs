namespace PaymentGateway.Api.Configuration;

public class BankSettings
{
    public const string SectionName = "Bank";

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 5;
}