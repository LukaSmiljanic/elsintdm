namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class PriceHistory : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public string? ChangedBy { get; set; }
}
