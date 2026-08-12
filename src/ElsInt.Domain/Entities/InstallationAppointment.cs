namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class InstallationAppointment : BaseEntity
{
    public Guid InstallationRequestId { get; set; }
    public InstallationRequest InstallationRequest { get; set; } = null!;

    public Guid InstallationTeamId { get; set; }
    public InstallationTeam InstallationTeam { get; set; } = null!;

    public DateTime ScheduledStartUtc { get; set; }
    public DateTime ScheduledEndUtc { get; set; }
    public string? Notes { get; set; }
    public bool IsCompleted { get; set; }
}
