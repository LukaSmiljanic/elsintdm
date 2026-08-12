namespace ElsInt.Domain.Entities;

using ElsInt.Domain.Common;

public class StockItem : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public int Quantity { get; set; }
    public int Reserved { get; set; }

    public int Available => Quantity - Reserved;

    public void Reserve(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        if (Available < amount)
            throw new InvalidOperationException($"Insufficient stock for product {ProductId} in warehouse {WarehouseId}.");
        Reserved += amount;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Release(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        if (Reserved < amount)
            throw new InvalidOperationException("Cannot release more than reserved.");
        Reserved -= amount;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void CommitReservation(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        if (Reserved < amount || Quantity < amount)
            throw new InvalidOperationException("Cannot commit reservation.");
        Reserved -= amount;
        Quantity -= amount;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
