namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class ConsentRecord : BaseEntity
{
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string ConsentType { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = string.Empty;
    public bool Accepted { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
