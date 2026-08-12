namespace ElsInt.Application.Interfaces;

public interface IStockService
{
    Task<Guid> ReserveAsync(Guid productId, int quantity, Guid? preferredWarehouseId, CancellationToken cancellationToken = default);
    Task ReleaseAsync(Guid productId, int quantity, Guid warehouseId, CancellationToken cancellationToken = default);
    Task CommitAsync(Guid productId, int quantity, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<Guid?> FindWarehouseWithStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
}
