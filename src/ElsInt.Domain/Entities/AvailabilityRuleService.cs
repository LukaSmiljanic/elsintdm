namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class AvailabilityRuleService : BaseEntity
{
    public Guid AvailabilityRuleId { get; set; }
    public AvailabilityRule AvailabilityRule { get; set; } = null!;

    public Guid FieldServiceId { get; set; }
    public FieldService FieldService { get; set; } = null!;
}
