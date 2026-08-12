namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;
using ElsInt.Domain.Enums;

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RSD";
    public string? ExternalTransactionId { get; set; }
    public string? ProviderPayload { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}
