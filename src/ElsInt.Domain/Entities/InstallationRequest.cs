namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class InstallationRequest : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public DateTime? PreferredDate { get; set; }
    public string? Notes { get; set; }
    public decimal Fee { get; set; }

    public InstallationAppointment? Appointment { get; set; }
}
