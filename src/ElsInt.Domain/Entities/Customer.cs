namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class Customer : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public bool IsGuest { get; set; } = true;
    public bool MarketingConsent { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "RS";

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<ConsentRecord> ConsentRecords { get; set; } = new List<ConsentRecord>();
}
