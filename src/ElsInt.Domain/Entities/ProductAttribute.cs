namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class ProductAttribute : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
