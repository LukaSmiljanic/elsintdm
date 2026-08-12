using ElsInt.Domain.Entities;
using ElsInt.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Infrastructure.Tests;

public class CatalogPriceListSeedTests
{
    private static async Task<AppDbContext> NewDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        db.Warehouses.Add(new Warehouse { Name = "Novi Sad", Code = "NS-01", City = "Novi Sad", IsActive = true });
        foreach (var (name, slug) in new[]
                 {
                     ("Split sistemi", "split-sistemi"),
                     ("Multi-split", "multi-split"),
                     ("Kaseta", "kaseta"),
                     ("Kanalski", "kanalski"),
                     ("Mobilni", "mobilni"),
                     ("VRF/VRV", "vrf-vrv")
                 })
        {
            db.Categories.Add(new Category { Name = name, Slug = slug });
        }
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task FirstRunImportsPriceListProducts()
    {
        await using var db = await NewDbAsync();

        await CatalogPriceListSeed.SeedAsync(db);

        (await db.Products.CountAsync()).Should().BeGreaterThan(0);
        (await db.ProductImages.CountAsync()).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SecondRunKeepsAdminEdits()
    {
        await using var db = await NewDbAsync();
        await CatalogPriceListSeed.SeedAsync(db);

        var product = await db.Products.OrderBy(p => p.Sku).FirstAsync();
        product.Price = 123_456m;
        product.Name = "Naziv iz admina";
        product.IsActive = false;
        await db.SaveChangesAsync();

        await CatalogPriceListSeed.SeedAsync(db);

        var reloaded = await db.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);
        reloaded.Price.Should().Be(123_456m);
        reloaded.Name.Should().Be("Naziv iz admina");
        reloaded.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SecondRunKeepsAdminUploadedImage()
    {
        await using var db = await NewDbAsync();
        await CatalogPriceListSeed.SeedAsync(db);

        var product = await db.Products.Include(p => p.Images).OrderBy(p => p.Sku).FirstAsync();
        foreach (var image in product.Images)
            db.ProductImages.Remove(image);
        var uploaded = new ProductImage
        {
            ProductId = product.Id,
            Url = "/images/db/11111111-1111-1111-1111-111111111111",
            IsPrimary = true
        };
        db.ProductImages.Add(uploaded);
        await db.SaveChangesAsync();

        await CatalogPriceListSeed.SeedAsync(db);

        var images = await db.ProductImages.AsNoTracking().Where(i => i.ProductId == product.Id).ToListAsync();
        images.Should().ContainSingle();
        images[0].Url.Should().Be(uploaded.Url);
    }
}
