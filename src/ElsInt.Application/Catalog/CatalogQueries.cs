using ElsInt.Application.Interfaces;
using ElsInt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Application.Catalog;

public record ProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string Sku,
    decimal Price,
    string BrandName,
    string CategoryName,
    decimal CoolingCapacityKw,
    decimal HeatingCapacityKw,
    int CoolingBtu,
    EnergyClass EnergyClassCooling,
    bool IsInverter,
    bool HasWifi,
    decimal NoiseLevelDb,
    decimal CoverageAreaSqm,
    string? PrimaryImageUrl,
    int AvailableStock);

public record CatalogBrandDto(Guid Id, string Name, string Slug);

public record ProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string Sku,
    string? ShortDescription,
    string? Description,
    string? MetaTitle,
    string? MetaDescription,
    decimal Price,
    decimal VatRate,
    string BrandName,
    string BrandSlug,
    string CategoryName,
    string CategorySlug,
    decimal CoolingCapacityKw,
    decimal HeatingCapacityKw,
    int CoolingBtu,
    int HeatingBtu,
    EnergyClass EnergyClassCooling,
    EnergyClass EnergyClassHeating,
    bool IsInverter,
    bool HasWifi,
    decimal NoiseLevelDb,
    decimal CoverageAreaSqm,
    CoolingType CoolingType,
    IReadOnlyList<ProductImageDto> Images,
    IReadOnlyList<ProductAttributeDto> Attributes,
    int AvailableStock);

public record ProductImageDto(string Url, string? AltText, bool IsPrimary);
public record ProductAttributeDto(string Name, string Value);
public record CategoryDto(Guid Id, string Name, string Slug, string? Description, string? MetaTitle, string? MetaDescription);
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly IAppDbContext _db;
    public GetCategoriesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.MetaTitle, c.MetaDescription))
            .ToListAsync(cancellationToken);
    }
}

public record GetProductsQuery(
    string? CategorySlug,
    string? BrandSlug,
    decimal? MinPrice,
    decimal? MaxPrice,
    decimal? MinCoolingKw,
    decimal? MaxCoolingKw,
    int? CoolingBtu,
    EnergyClass? EnergyClass,
    bool? IsInverter,
    bool? HasWifi,
    decimal? MinCoverageSqm,
    decimal? MaxNoiseDb,
    string? Search,
    string? SortBy,
    int Page = 1,
    int PageSize = 12) : IRequest<PagedResult<ProductListItemDto>>;

public record GetCatalogBrandsQuery : IRequest<IReadOnlyList<CatalogBrandDto>>;

public class GetCatalogBrandsQueryHandler : IRequestHandler<GetCatalogBrandsQuery, IReadOnlyList<CatalogBrandDto>>
{
    private readonly IAppDbContext _db;
    public GetCatalogBrandsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CatalogBrandDto>> Handle(GetCatalogBrandsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Brands
            .AsNoTracking()
            .Where(b => b.IsActive && b.Products.Any(p => p.IsActive && p.Price > 0 && p.CoolingBtu > 0))
            .OrderBy(b => b.Name)
            .Select(b => new CatalogBrandDto(b.Id, b.Name, b.Slug))
            .ToListAsync(cancellationToken);
    }
}

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductListItemDto>>
{
    private readonly IAppDbContext _db;
    public GetProductsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<ProductListItemDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.StockItems)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
            query = query.Where(p => p.Category.Slug == request.CategorySlug);
        if (!string.IsNullOrWhiteSpace(request.BrandSlug))
            query = query.Where(p => p.Brand.Slug == request.BrandSlug);
        if (request.MinPrice.HasValue)
            query = query.Where(p => p.Price >= request.MinPrice.Value);
        if (request.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= request.MaxPrice.Value);
        if (request.MinCoolingKw.HasValue)
            query = query.Where(p => p.CoolingCapacityKw >= request.MinCoolingKw.Value);
        if (request.MaxCoolingKw.HasValue)
            query = query.Where(p => p.CoolingCapacityKw <= request.MaxCoolingKw.Value);
        if (request.CoolingBtu.HasValue)
            query = query.Where(p => p.CoolingBtu == request.CoolingBtu.Value);
        if (request.EnergyClass.HasValue)
            query = query.Where(p => p.EnergyClassCooling == request.EnergyClass.Value);
        if (request.IsInverter.HasValue)
            query = query.Where(p => p.IsInverter == request.IsInverter.Value);
        if (request.HasWifi.HasValue)
            query = query.Where(p => p.HasWifi == request.HasWifi.Value);
        if (request.MinCoverageSqm.HasValue)
            query = query.Where(p => p.CoverageAreaSqm >= request.MinCoverageSqm.Value);
        if (request.MaxNoiseDb.HasValue)
            query = query.Where(p => p.NoiseLevelDb <= request.MaxNoiseDb.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(p => p.Name.Contains(s) || p.Sku.Contains(s) || p.Brand.Name.Contains(s));
        }

        query = request.SortBy?.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name" => query.OrderBy(p => p.Name),
            "power" => query.OrderBy(p => p.CoolingCapacityKw),
            _ => query.OrderByDescending(p => p.CreatedAtUtc)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListItemDto(
                p.Id,
                p.Name,
                p.Slug,
                p.Sku,
                p.Price,
                p.Brand.Name,
                p.Category.Name,
                p.CoolingCapacityKw,
                p.HeatingCapacityKw,
                p.CoolingBtu,
                p.EnergyClassCooling,
                p.IsInverter,
                p.HasWifi,
                p.NoiseLevelDb,
                p.CoverageAreaSqm,
                p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault()
                    ?? p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.StockItems.Sum(s => s.Quantity - s.Reserved)))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductListItemDto>(items, total, page, pageSize);
    }
}

public record GetProductBySlugQuery(string Slug) : IRequest<ProductDetailDto?>;

public class GetProductBySlugQueryHandler : IRequestHandler<GetProductBySlugQuery, ProductDetailDto?>
{
    private readonly IAppDbContext _db;
    public GetProductBySlugQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ProductDetailDto?> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var p = await _db.Products
            .AsNoTracking()
            .Include(x => x.Brand)
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Include(x => x.Attributes)
            .Include(x => x.StockItems)
            .FirstOrDefaultAsync(x => x.Slug == request.Slug && x.IsActive, cancellationToken);

        if (p is null) return null;

        return new ProductDetailDto(
            p.Id, p.Name, p.Slug, p.Sku, p.ShortDescription, p.Description,
            p.MetaTitle, p.MetaDescription, p.Price, p.VatRate,
            p.Brand.Name, p.Brand.Slug, p.Category.Name, p.Category.Slug,
            p.CoolingCapacityKw, p.HeatingCapacityKw, p.CoolingBtu, p.HeatingBtu,
            p.EnergyClassCooling, p.EnergyClassHeating, p.IsInverter, p.HasWifi,
            p.NoiseLevelDb, p.CoverageAreaSqm, p.CoolingType,
            p.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto(i.Url, i.AltText, i.IsPrimary)).ToList(),
            p.Attributes
                .Where(a => !a.Name.Contains("MP") && !a.Name.Contains("serviser") && !a.Name.Contains("akcij"))
                .OrderBy(a => a.SortOrder)
                .Select(a => new ProductAttributeDto(a.Name, a.Value))
                .ToList(),
            p.StockItems.Sum(s => s.Quantity - s.Reserved));
    }
}
