using ElsInt.Application.Common;
using ElsInt.Application.Interfaces;
using ElsInt.Domain.Entities;
using ElsInt.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Application.Admin;

public record AdminProductDto(
    Guid Id, string Sku, string Name, string Slug, decimal Price, bool IsActive,
    Guid BrandId, Guid CategoryId, decimal CoolingCapacityKw, decimal CoverageAreaSqm,
    bool IsInverter, bool HasWifi, EnergyClass EnergyClassCooling, int TotalStock,
    string? PrimaryImageUrl);

public record UpsertProductCommand(
    Guid? Id,
    string Sku,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    string? MetaTitle,
    string? MetaDescription,
    Guid BrandId,
    Guid CategoryId,
    decimal Price,
    bool IsActive,
    decimal CoolingCapacityKw,
    decimal HeatingCapacityKw,
    EnergyClass EnergyClassCooling,
    EnergyClass EnergyClassHeating,
    bool IsInverter,
    bool HasWifi,
    decimal NoiseLevelDb,
    decimal CoverageAreaSqm,
    CoolingType CoolingType) : IRequest<Guid>;

public class UpsertProductCommandValidator : AbstractValidator<UpsertProductCommand>
{
    public UpsertProductCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(220);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class UpsertProductCommandHandler : IRequestHandler<UpsertProductCommand, Guid>
{
    private readonly IAppDbContext _db;
    public UpsertProductCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(UpsertProductCommand request, CancellationToken cancellationToken)
    {
        var sku = request.Sku.Trim();
        var slug = request.Slug.Trim();

        if (await _db.Products.AnyAsync(p => p.Sku == sku && p.Id != request.Id, cancellationToken))
            throw new InvalidOperationException($"SKU \"{sku}\" već postoji.");
        if (await _db.Products.AnyAsync(p => p.Slug == slug && p.Id != request.Id, cancellationToken))
            throw new InvalidOperationException($"Slug \"{slug}\" već postoji.");

        Product product;
        if (request.Id.HasValue)
        {
            product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Product not found.");

            if (product.Price != request.Price)
            {
                _db.PriceHistories.Add(new PriceHistory
                {
                    ProductId = product.Id,
                    OldPrice = product.Price,
                    NewPrice = request.Price,
                    ChangedBy = "admin"
                });
            }
        }
        else
        {
            product = new Product();
            _db.Products.Add(product);
        }

        product.Sku = sku;
        product.Name = request.Name.Trim();
        product.Slug = slug;
        product.ShortDescription = request.ShortDescription;
        product.Description = request.Description;
        product.MetaTitle = string.IsNullOrWhiteSpace(request.MetaTitle)
            ? $"{product.Name} - cena i montaža"
            : request.MetaTitle;
        product.MetaDescription = string.IsNullOrWhiteSpace(request.MetaDescription)
            ? $"Kupite {product.Name} kod ElsInt. Cena {PriceText.Rsd(request.Price)} RSD sa PDV. Prodaja klima uređaja, dostava i profesionalna montaža u Srbiji."
            : request.MetaDescription;
        product.BrandId = request.BrandId;
        product.CategoryId = request.CategoryId;
        product.Price = request.Price;
        product.IsActive = request.IsActive;
        product.CoolingCapacityKw = request.CoolingCapacityKw;
        product.HeatingCapacityKw = request.HeatingCapacityKw;
        product.CoolingBtu = CapacityMath.BtuFromKw(request.CoolingCapacityKw);
        product.HeatingBtu = CapacityMath.BtuFromKw(request.HeatingCapacityKw);
        product.EnergyClassCooling = request.EnergyClassCooling;
        product.EnergyClassHeating = request.EnergyClassHeating;
        product.IsInverter = request.IsInverter;
        product.HasWifi = request.HasWifi;
        product.NoiseLevelDb = request.NoiseLevelDb;
        product.CoverageAreaSqm = request.CoverageAreaSqm;
        product.CoolingType = request.CoolingType;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}

public record GetAdminProductsQuery(string? Search, int Page = 1, int PageSize = 20) : IRequest<Catalog.PagedResult<AdminProductDto>>;

public class GetAdminProductsQueryHandler : IRequestHandler<GetAdminProductsQuery, Catalog.PagedResult<AdminProductDto>>
{
    private readonly IAppDbContext _db;
    public GetAdminProductsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<Catalog.PagedResult<AdminProductDto>> Handle(GetAdminProductsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = _db.Products.AsNoTracking().Include(p => p.StockItems).AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(p => p.Name.Contains(s) || p.Sku.Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new AdminProductDto(
                p.Id, p.Sku, p.Name, p.Slug, p.Price, p.IsActive, p.BrandId, p.CategoryId,
                p.CoolingCapacityKw, p.CoverageAreaSqm, p.IsInverter, p.HasWifi, p.EnergyClassCooling,
                p.StockItems.Sum(s => s.Quantity),
                p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
                    .Select(i => i.Url).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new Catalog.PagedResult<AdminProductDto>(items, total, page, pageSize);
    }
}

public record AdminProductDetailDto(
    Guid Id, string Sku, string Name, string Slug, string? ShortDescription, string? Description,
    string? MetaTitle, string? MetaDescription, Guid BrandId, Guid CategoryId, decimal Price, bool IsActive,
    decimal CoolingCapacityKw, decimal HeatingCapacityKw, int CoolingBtu, int HeatingBtu,
    EnergyClass EnergyClassCooling, EnergyClass EnergyClassHeating, bool IsInverter, bool HasWifi,
    decimal NoiseLevelDb, decimal CoverageAreaSqm, CoolingType CoolingType, int TotalStock);

public record GetAdminProductByIdQuery(Guid Id) : IRequest<AdminProductDetailDto>;

public class GetAdminProductByIdQueryHandler : IRequestHandler<GetAdminProductByIdQuery, AdminProductDetailDto>
{
    private readonly IAppDbContext _db;
    public GetAdminProductByIdQueryHandler(IAppDbContext db) => _db = db;

    public async Task<AdminProductDetailDto> Handle(GetAdminProductByIdQuery request, CancellationToken cancellationToken)
    {
        var p = await _db.Products.AsNoTracking()
            .Include(x => x.StockItems)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Product not found.");

        return new AdminProductDetailDto(
            p.Id, p.Sku, p.Name, p.Slug, p.ShortDescription, p.Description, p.MetaTitle, p.MetaDescription,
            p.BrandId, p.CategoryId, p.Price, p.IsActive, p.CoolingCapacityKw, p.HeatingCapacityKw,
            p.CoolingBtu, p.HeatingBtu, p.EnergyClassCooling, p.EnergyClassHeating, p.IsInverter, p.HasWifi,
            p.NoiseLevelDb, p.CoverageAreaSqm, p.CoolingType, p.StockItems.Sum(s => s.Quantity));
    }
}

public record UpdateAdminProductCommand(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    Guid BrandId,
    Guid CategoryId,
    decimal Price,
    bool IsActive,
    decimal CoolingCapacityKw,
    decimal HeatingCapacityKw,
    EnergyClass EnergyClassCooling,
    EnergyClass EnergyClassHeating,
    bool IsInverter,
    bool HasWifi,
    decimal NoiseLevelDb,
    decimal CoverageAreaSqm,
    CoolingType CoolingType) : IRequest;

public class UpdateAdminProductCommandValidator : AbstractValidator<UpdateAdminProductCommand>
{
    public UpdateAdminProductCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(220);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class UpdateAdminProductCommandHandler : IRequestHandler<UpdateAdminProductCommand>
{
    private readonly IAppDbContext _db;
    public UpdateAdminProductCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateAdminProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Product not found.");

        var sku = request.Sku.Trim();
        var slug = request.Slug.Trim();

        if (await _db.Products.AnyAsync(p => p.Sku == sku && p.Id != product.Id, cancellationToken))
            throw new InvalidOperationException($"SKU \"{sku}\" već postoji.");
        if (await _db.Products.AnyAsync(p => p.Slug == slug && p.Id != product.Id, cancellationToken))
            throw new InvalidOperationException($"Slug \"{slug}\" već postoji.");

        if (product.Price != request.Price)
        {
            _db.PriceHistories.Add(new PriceHistory
            {
                ProductId = product.Id,
                OldPrice = product.Price,
                NewPrice = request.Price,
                ChangedBy = "admin"
            });
        }

        product.Sku = sku;
        product.Name = request.Name.Trim();
        product.Slug = slug;
        product.ShortDescription = request.ShortDescription;
        product.Description = request.Description;
        product.BrandId = request.BrandId;
        product.CategoryId = request.CategoryId;
        product.Price = request.Price;
        product.IsActive = request.IsActive;
        product.CoolingCapacityKw = request.CoolingCapacityKw;
        product.HeatingCapacityKw = request.HeatingCapacityKw;
        product.CoolingBtu = CapacityMath.BtuFromKw(request.CoolingCapacityKw);
        product.HeatingBtu = CapacityMath.BtuFromKw(request.HeatingCapacityKw);
        product.EnergyClassCooling = request.EnergyClassCooling;
        product.EnergyClassHeating = request.EnergyClassHeating;
        product.IsInverter = request.IsInverter;
        product.HasWifi = request.HasWifi;
        product.NoiseLevelDb = request.NoiseLevelDb;
        product.CoverageAreaSqm = request.CoverageAreaSqm;
        product.CoolingType = request.CoolingType;
        product.MetaTitle = $"{request.Name} - cena i montaža";
        product.MetaDescription =
            $"Kupite {request.Name} kod ElsInt. Cena {PriceText.Rsd(request.Price)} RSD sa PDV. Prodaja klima uređaja, dostava i profesionalna montaža u Srbiji.";
        product.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }
}

public record AdminCategoryDto(Guid Id, string Name, string Slug, bool IsActive);
public record GetAdminCategoriesQuery : IRequest<IReadOnlyList<AdminCategoryDto>>;

public class GetAdminCategoriesQueryHandler : IRequestHandler<GetAdminCategoriesQuery, IReadOnlyList<AdminCategoryDto>>
{
    private readonly IAppDbContext _db;
    public GetAdminCategoriesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AdminCategoryDto>> Handle(GetAdminCategoriesQuery request, CancellationToken cancellationToken)
        => await _db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new AdminCategoryDto(c.Id, c.Name, c.Slug, c.IsActive))
            .ToListAsync(cancellationToken);
}

public record AdjustStockCommand(Guid ProductId, Guid WarehouseId, int Quantity, int? Reserved) : IRequest;

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand>
{
    private readonly IAppDbContext _db;
    public AdjustStockCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var stock = await _db.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == request.ProductId && s.WarehouseId == request.WarehouseId, cancellationToken);

        if (stock is null)
        {
            stock = new StockItem
            {
                ProductId = request.ProductId,
                WarehouseId = request.WarehouseId,
                Quantity = request.Quantity,
                Reserved = request.Reserved ?? 0
            };
            _db.StockItems.Add(stock);
        }
        else
        {
            stock.Quantity = request.Quantity;
            if (request.Reserved.HasValue)
                stock.Reserved = request.Reserved.Value;
            stock.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}

public record OrderListItemDto(Guid Id, string OrderNumber, OrderStatus Status, decimal Total, string CustomerEmail, DateTime CreatedAtUtc, DeliveryOption DeliveryOption);

public record GetAdminOrdersQuery(OrderStatus? Status, int Page = 1, int PageSize = 20) : IRequest<Catalog.PagedResult<OrderListItemDto>>;

public class GetAdminOrdersQueryHandler : IRequestHandler<GetAdminOrdersQuery, Catalog.PagedResult<OrderListItemDto>>
{
    private readonly IAppDbContext _db;
    public GetAdminOrdersQueryHandler(IAppDbContext db) => _db = db;

    public async Task<Catalog.PagedResult<OrderListItemDto>> Handle(GetAdminOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = _db.Orders.AsNoTracking().Include(o => o.Customer).AsQueryable();
        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new OrderListItemDto(o.Id, o.OrderNumber, o.Status, o.Total, o.Customer.Email, o.CreatedAtUtc, o.DeliveryOption))
            .ToListAsync(cancellationToken);

        return new Catalog.PagedResult<OrderListItemDto>(items, total, page, pageSize);
    }
}

public record UpdateOrderStatusCommand(Guid OrderId, OrderStatus Status) : IRequest;

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand>
{
    private readonly IAppDbContext _db;
    private readonly IStockService _stock;

    public UpdateOrderStatusCommandHandler(IAppDbContext db, IStockService stock)
    {
        _db = db;
        _stock = stock;
    }

    public async Task Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        var previous = order.Status;
        order.TransitionTo(request.Status);

        if (request.Status == OrderStatus.Cancelled && order.WarehouseId.HasValue &&
            previous is OrderStatus.PendingPayment or OrderStatus.Processing)
        {
            foreach (var item in order.Items)
                await _stock.ReleaseAsync(item.ProductId, item.Quantity, order.WarehouseId.Value, cancellationToken);
        }

        if (request.Status == OrderStatus.Shipped && order.WarehouseId.HasValue && previous == OrderStatus.Processing)
        {
            foreach (var item in order.Items)
                await _stock.CommitAsync(item.ProductId, item.Quantity, order.WarehouseId.Value, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}

public record UpsertPromotionCommand(
    Guid? Id,
    string Name,
    string? Badge,
    decimal? DiscountPercent,
    decimal? DiscountAmount,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    bool IsActive,
    IReadOnlyList<Guid> ProductIds) : IRequest<Guid>;

public class UpsertPromotionCommandHandler : IRequestHandler<UpsertPromotionCommand, Guid>
{
    private readonly IAppDbContext _db;
    public UpsertPromotionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(UpsertPromotionCommand request, CancellationToken cancellationToken)
    {
        Promotion promo;
        if (request.Id.HasValue)
        {
            promo = await _db.Promotions.Include(p => p.PromotionProducts)
                .FirstOrDefaultAsync(p => p.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Promotion not found.");
            _db.PromotionProducts.RemoveRange(promo.PromotionProducts);
        }
        else
        {
            promo = new Promotion();
            _db.Promotions.Add(promo);
        }

        promo.Name = request.Name;
        promo.Badge = request.Badge;
        promo.DiscountPercent = request.DiscountPercent;
        promo.DiscountAmount = request.DiscountAmount;
        promo.StartsAtUtc = request.StartsAtUtc;
        promo.EndsAtUtc = request.EndsAtUtc;
        promo.IsActive = request.IsActive;
        promo.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var productId in request.ProductIds.Distinct())
        {
            promo.PromotionProducts.Add(new PromotionProduct { ProductId = productId });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return promo.Id;
    }
}

public record GetWarehousesQuery : IRequest<IReadOnlyList<WarehouseDto>>;
public record WarehouseDto(Guid Id, string Name, string Code, string? City, bool IsActive);

public class GetWarehousesQueryHandler : IRequestHandler<GetWarehousesQuery, IReadOnlyList<WarehouseDto>>
{
    private readonly IAppDbContext _db;
    public GetWarehousesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Warehouses.AsNoTracking()
            .Where(w => w.IsActive)
            .OrderBy(w => w.Name)
            .Select(w => new WarehouseDto(w.Id, w.Name, w.Code, w.City, w.IsActive))
            .ToListAsync(cancellationToken);
    }
}

public record GetStockQuery(Guid? WarehouseId = null) : IRequest<IReadOnlyList<StockDto>>;
public record StockDto(Guid ProductId, string ProductName, string Sku, Guid WarehouseId, string WarehouseName, int Quantity, int Reserved, int Available);

public class GetStockQueryHandler : IRequestHandler<GetStockQuery, IReadOnlyList<StockDto>>
{
    private readonly IAppDbContext _db;
    public GetStockQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StockDto>> Handle(GetStockQuery request, CancellationToken cancellationToken)
    {
        var query = _db.StockItems.AsNoTracking()
            .Include(s => s.Product)
            .Include(s => s.Warehouse)
            .Where(s => s.Warehouse.IsActive);

        if (request.WarehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == request.WarehouseId.Value);

        return await query
            .OrderBy(s => s.Product.Name)
            .Select(s => new StockDto(s.ProductId, s.Product.Name, s.Product.Sku, s.WarehouseId, s.Warehouse.Name, s.Quantity, s.Reserved, s.Quantity - s.Reserved))
            .ToListAsync(cancellationToken);
    }
}

public record GetBrandsQuery : IRequest<IReadOnlyList<BrandDto>>;
public record BrandDto(Guid Id, string Name, string Slug, bool IsActive);

public class GetBrandsQueryHandler : IRequestHandler<GetBrandsQuery, IReadOnlyList<BrandDto>>
{
    private readonly IAppDbContext _db;
    public GetBrandsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<BrandDto>> Handle(GetBrandsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Brands.AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BrandDto(b.Id, b.Name, b.Slug, b.IsActive))
            .ToListAsync(cancellationToken);
    }
}

public record RecordConsentCommand(string ConsentType, string PolicyVersion, bool Accepted, string? IpAddress, string? UserAgent, Guid? CustomerId) : IRequest;

public class RecordConsentCommandHandler : IRequestHandler<RecordConsentCommand>
{
    private readonly IAppDbContext _db;
    public RecordConsentCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(RecordConsentCommand request, CancellationToken cancellationToken)
    {
        _db.ConsentRecords.Add(new ConsentRecord
        {
            CustomerId = request.CustomerId,
            ConsentType = request.ConsentType,
            PolicyVersion = request.PolicyVersion,
            Accepted = request.Accepted,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
