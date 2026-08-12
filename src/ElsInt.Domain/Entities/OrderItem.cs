namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
