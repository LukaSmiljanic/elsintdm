namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

/// <summary>
/// Weekly recurring availability window. DayOfWeek: 0 = Sunday ... 6 = Saturday (same as .NET).
/// If RuleLinks is empty, all active services are allowed in this window.
/// </summary>
public class AvailabilityRule : BaseEntity
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Label { get; set; }

    public ICollection<AvailabilityRuleService> ServiceLinks { get; set; } = new List<AvailabilityRuleService>();
}
