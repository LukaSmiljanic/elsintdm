namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;
using ElsInt.Domain.Enums;

public class Product : BaseEntity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public Guid BrandId { get; set; }
    public Brand Brand { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public decimal Price { get; set; }
    public decimal VatRate { get; set; } = 0.20m;
    public bool IsActive { get; set; } = true;

    public decimal CoolingCapacityKw { get; set; }
    public decimal HeatingCapacityKw { get; set; }
    public int CoolingBtu { get; set; }
    public int HeatingBtu { get; set; }
    public EnergyClass EnergyClassCooling { get; set; }
    public EnergyClass EnergyClassHeating { get; set; }
    public bool IsInverter { get; set; }
    public bool HasWifi { get; set; }
    public decimal NoiseLevelDb { get; set; }
    public decimal CoverageAreaSqm { get; set; }
    public CoolingType CoolingType { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    public ICollection<PriceHistory> PriceHistory { get; set; } = new List<PriceHistory>();
    public ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
