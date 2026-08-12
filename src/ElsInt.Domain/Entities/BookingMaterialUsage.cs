namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

/// <summary>Material spent on a booking (montaža/servis). Recording it deducts warehouse stock.</summary>
public class BookingMaterialUsage : BaseEntity
{
    public Guid ServiceBookingId { get; set; }
    public ServiceBooking ServiceBooking { get; set; } = null!;

    public Guid MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}
