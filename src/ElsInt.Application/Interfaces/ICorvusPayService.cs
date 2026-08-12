namespace ElsInt.Application.Interfaces;

public interface ICorvusPayService
{
    Task<CorvusPayRedirectResult> CreatePaymentAsync(CorvusPayPaymentRequest request, CancellationToken cancellationToken = default);
    bool ValidateCallback(IReadOnlyDictionary<string, string> formFields);
}

public record CorvusPayPaymentRequest(
    string OrderNumber,
    decimal Amount,
    string Currency,
    string SuccessUrl,
    string CancelUrl,
    string Language = "sr");

public record CorvusPayRedirectResult(string RedirectUrl, string? TransactionId);
