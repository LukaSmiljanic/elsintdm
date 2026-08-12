namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class InstallationTeam : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<InstallationAppointment> Appointments { get; set; } = new List<InstallationAppointment>();
}
