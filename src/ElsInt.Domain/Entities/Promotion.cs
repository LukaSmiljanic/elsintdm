namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class Promotion : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Badge { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
}
