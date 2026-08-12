namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class Warehouse : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    public ICollection<MaterialStockItem> MaterialStockItems { get; set; } = new List<MaterialStockItem>();
}
