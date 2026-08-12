namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class MaterialStockItem : BaseEntity
{
    public Guid MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public decimal Quantity { get; set; }
}
