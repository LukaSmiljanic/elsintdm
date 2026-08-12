using ElsInt.Domain.Entities;
using ElsInt.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElsInt.Infrastructure.Tests;

public class StockServiceTests
{
    private static async Task<(AppDbContext db, Guid productId, Guid warehouseId)> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var brand = new Brand { Name = "Gree", Slug = "gree" };
        var cat = new Category { Name = "Split", Slug = "split" };
        var warehouse = new Warehouse { Name = "BG", Code = "BG-01" };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();

        var product = new Product
        {
            Name = "Test",
            Slug = "test",
            Sku = "T-1",
            BrandId = brand.Id,
            CategoryId = cat.Id,
            Price = 1000
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.StockItems.Add(new StockItem
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Quantity = 10,
            Reserved = 0
        });
        await db.SaveChangesAsync();
        return (db, product.Id, warehouse.Id);
    }

    [Fact]
    public async Task Reserve_ThenRelease_RestoresAvailability()
    {
        var (db, productId, warehouseId) = await SeedAsync();
        await using (db)
        {
            var service = new StockService(db, NullLogger<StockService>.Instance);
            var wh = await service.ReserveAsync(productId, 3, null);
            wh.Should().Be(warehouseId);

            var stock = await db.StockItems.SingleAsync();
            stock.Reserved.Should().Be(3);
            stock.Available.Should().Be(7);

            await service.ReleaseAsync(productId, 3, warehouseId);
            stock = await db.StockItems.SingleAsync();
            stock.Reserved.Should().Be(0);
            stock.Available.Should().Be(10);
        }
    }

    [Fact]
    public async Task Reserve_WhenInsufficient_Throws()
    {
        var (db, productId, _) = await SeedAsync();
        await using (db)
        {
            var service = new StockService(db, NullLogger<StockService>.Instance);
            var act = () => service.ReserveAsync(productId, 50, null);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }

    [Fact]
    public async Task Commit_ReducesQuantity()
    {
        var (db, productId, warehouseId) = await SeedAsync();
        await using (db)
        {
            var service = new StockService(db, NullLogger<StockService>.Instance);
            await service.ReserveAsync(productId, 4, warehouseId);
            await service.CommitAsync(productId, 4, warehouseId);
            var stock = await db.StockItems.SingleAsync();
            stock.Quantity.Should().Be(6);
            stock.Reserved.Should().Be(0);
        }
    }
}
