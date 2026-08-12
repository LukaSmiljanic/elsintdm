namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class FieldService : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<AvailabilityRuleService> RuleLinks { get; set; } = new List<AvailabilityRuleService>();
    public ICollection<ServiceBooking> Bookings { get; set; } = new List<ServiceBooking>();
}
