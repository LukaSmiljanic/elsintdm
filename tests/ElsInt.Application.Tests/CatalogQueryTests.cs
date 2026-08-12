using ElsInt.Application.Catalog;
using ElsInt.Domain.Entities;
using ElsInt.Domain.Enums;
using ElsInt.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Application.Tests;

public class CatalogQueryTests
{
    private static async Task<AppDbContext> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var brand = new Brand { Name = "Daikin", Slug = "daikin" };
        var catSplit = new Category { Name = "Split", Slug = "split-sistemi", IsActive = true };
        var catMobile = new Category { Name = "Mobilni", Slug = "mobilni", IsActive = true };
        db.Brands.Add(brand);
        db.Categories.AddRange(catSplit, catMobile);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Product
            {
                Name = "Daikin 2.5",
                Slug = "daikin-25",
                Sku = "DK-25",
                BrandId = brand.Id,
                CategoryId = catSplit.Id,
                Price = 90000,
                CoolingCapacityKw = 2.5m,
                CoverageAreaSqm = 25,
                IsInverter = true,
                HasWifi = true,
                EnergyClassCooling = EnergyClass.Aplusplus,
                IsActive = true,
                NoiseLevelDb = 19
            },
            new Product
            {
                Name = "Daikin Mobile",
                Slug = "daikin-mobile",
                Sku = "DK-M",
                BrandId = brand.Id,
                CategoryId = catMobile.Id,
                Price = 50000,
                CoolingCapacityKw = 2.0m,
                CoverageAreaSqm = 18,
                IsInverter = false,
                HasWifi = false,
                EnergyClassCooling = EnergyClass.A,
                IsActive = true,
                NoiseLevelDb = 45
            });
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task GetProducts_FiltersByCategoryAndInverter()
    {
        await using var db = await SeedAsync();
        var handler = new GetProductsQueryHandler(db);
        var result = await handler.Handle(new GetProductsQuery(
            CategorySlug: "split-sistemi",
            BrandSlug: null,
            MinPrice: null,
            MaxPrice: null,
            MinCoolingKw: null,
            MaxCoolingKw: null,
            CoolingBtu: null,
            EnergyClass: null,
            IsInverter: true,
            HasWifi: null,
            MinCoverageSqm: null,
            MaxNoiseDb: null,
            Search: null,
            SortBy: null,
            Page: 1,
            PageSize: 10), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items[0].Slug.Should().Be("daikin-25");
    }

    [Fact]
    public async Task GetProductBySlug_ReturnsDetail()
    {
        await using var db = await SeedAsync();
        var handler = new GetProductBySlugQueryHandler(db);
        var result = await handler.Handle(new GetProductBySlugQuery("daikin-25"), CancellationToken.None);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Daikin 2.5");
        result.BrandSlug.Should().Be("daikin");
    }

    [Fact]
    public async Task GetCategories_ReturnsActiveOnly()
    {
        await using var db = await SeedAsync();
        var handler = new GetCategoriesQueryHandler(db);
        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);
        result.Should().HaveCount(2);
    }
}
