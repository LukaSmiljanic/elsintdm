using System.Security.Cryptography;
using System.Text;
using ElsInt.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ElsInt.Infrastructure.Payments;

public class CorvusPayService : ICorvusPayService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CorvusPayService> _logger;

    public CorvusPayService(IConfiguration configuration, ILogger<CorvusPayService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<CorvusPayRedirectResult> CreatePaymentAsync(CorvusPayPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var storeId = _configuration["CorvusPay:StoreId"] ?? "sandbox-store";
        var secretKey = _configuration["CorvusPay:SecretKey"] ?? "sandbox-secret";
        var endpoint = _configuration["CorvusPay:Endpoint"] ?? "https://testcps.corvuspay.com/payment/form";

        var amount = ((int)Math.Round(request.Amount * 100)).ToString();
        var fields = new SortedDictionary<string, string>
        {
            ["store_id"] = storeId,
            ["order_number"] = request.OrderNumber,
            ["language"] = request.Language,
            ["currency"] = request.Currency == "RSD" ? "941" : "978",
            ["amount"] = amount,
            ["cart"] = $"ElsInt order {request.OrderNumber}",
            ["require_complete"] = "true",
            ["version"] = "1.4"
        };

        var signature = ComputeSignature(fields, secretKey);
        var query = string.Join("&", fields.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var redirectUrl = $"{endpoint}?{query}&signature={signature}";

        _logger.LogInformation("CorvusPay redirect created for order {OrderNumber}", request.OrderNumber);
        return Task.FromResult(new CorvusPayRedirectResult(redirectUrl, request.OrderNumber));
    }

    public bool ValidateCallback(IReadOnlyDictionary<string, string> formFields)
    {
        var secretKey = _configuration["CorvusPay:SecretKey"] ?? "sandbox-secret";
        if (!formFields.TryGetValue("signature", out var provided))
            return false;

        var fields = formFields
            .Where(kv => !string.Equals(kv.Key, "signature", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

        var expected = ComputeSignature(new SortedDictionary<string, string>(fields), secretKey);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected.ToLowerInvariant()),
            Encoding.UTF8.GetBytes(provided.ToLowerInvariant()));
    }

    private static string ComputeSignature(SortedDictionary<string, string> fields, string secretKey)
    {
        var data = string.Concat(fields.Select(kv => kv.Key + kv.Value));
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
