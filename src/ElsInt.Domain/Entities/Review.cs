namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class Review : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public bool IsApproved { get; set; }
}
