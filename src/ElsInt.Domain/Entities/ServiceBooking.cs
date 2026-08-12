namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;
using ElsInt.Domain.Enums;

public class ServiceBooking : BaseEntity
{
    public Guid FieldServiceId { get; set; }
    public FieldService FieldService { get; set; } = null!;

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    public ServiceBookingStatus Status { get; set; } = ServiceBookingStatus.Held;
    public DateTime? HoldExpiresAtUtc { get; set; }
    public string? AdminNotes { get; set; }

    public ICollection<BookingMaterialUsage> MaterialUsages { get; set; } = new List<BookingMaterialUsage>();
}
