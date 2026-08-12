using ElsInt.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ElsInt.Infrastructure.Persistence;

public class StockService : IStockService
{
    private readonly IAppDbContext _db;
    private readonly ILogger<StockService> _logger;

    public StockService(IAppDbContext db, ILogger<StockService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Guid?> FindWarehouseWithStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        var item = await _db.StockItems
            .Where(s => s.Warehouse.IsActive && s.ProductId == productId && s.Quantity - s.Reserved >= quantity)
            .OrderByDescending(s => s.Quantity - s.Reserved)
            .FirstOrDefaultAsync(cancellationToken);

        return item?.WarehouseId;
    }

    public async Task<Guid> ReserveAsync(Guid productId, int quantity, Guid? preferredWarehouseId, CancellationToken cancellationToken = default)
    {
        var query = _db.StockItems.Where(s =>
            s.Warehouse.IsActive && s.ProductId == productId && s.Quantity - s.Reserved >= quantity);
        if (preferredWarehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == preferredWarehouseId.Value);

        var stock = await query.OrderByDescending(s => s.Quantity - s.Reserved).FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"No stock available for product {productId}.");

        stock.Reserve(quantity);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Reserved {Qty} of {ProductId} in warehouse {WarehouseId}", quantity, productId, stock.WarehouseId);
        return stock.WarehouseId;
    }

    public async Task ReleaseAsync(Guid productId, int quantity, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var stock = await _db.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId, cancellationToken)
            ?? throw new InvalidOperationException("Stock item not found.");

        stock.Release(quantity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task CommitAsync(Guid productId, int quantity, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var stock = await _db.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId, cancellationToken)
            ?? throw new InvalidOperationException("Stock item not found.");

        stock.CommitReservation(quantity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
