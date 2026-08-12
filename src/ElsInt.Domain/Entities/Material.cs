namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class Material : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "kom";
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MaterialStockItem> StockItems { get; set; } = new List<MaterialStockItem>();
}
