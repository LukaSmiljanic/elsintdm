namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }

    /// <summary>Set for images uploaded through admin; served from the DB so they survive an API republish.</summary>
    public byte[]? Data { get; set; }
    public string? ContentType { get; set; }
    public string? FileName { get; set; }
}
