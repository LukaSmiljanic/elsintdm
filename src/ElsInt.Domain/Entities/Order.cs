namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;
using ElsInt.Domain.Enums;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public DeliveryOption DeliveryOption { get; set; }

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public string ShippingAddressLine1 { get; set; } = string.Empty;
    public string? ShippingAddressLine2 { get; set; }
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingPostalCode { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = "RS";
    public string? Notes { get; set; }

    public decimal Subtotal { get; set; }
    public decimal VatAmount { get; set; }
    public decimal InstallationFee { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public InstallationRequest? InstallationRequest { get; set; }

    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.PendingPayment] = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Installed, OrderStatus.Closed],
        [OrderStatus.Installed] = [OrderStatus.Closed],
        [OrderStatus.Closed] = [],
        [OrderStatus.Cancelled] = []
    };

    public void TransitionTo(OrderStatus newStatus)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException($"Cannot transition order from {Status} to {newStatus}.");

        Status = newStatus;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
