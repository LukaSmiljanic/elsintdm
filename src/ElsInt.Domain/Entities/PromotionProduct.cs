namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class PromotionProduct : BaseEntity
{
    public Guid PromotionId { get; set; }
    public Promotion Promotion { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
