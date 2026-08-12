using ElsInt.Application.Interfaces;
using ElsInt.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Application.Admin;

public record MaterialStockLineDto(Guid WarehouseId, string WarehouseName, decimal Quantity);

public record MaterialListItemDto(
    Guid Id,
    string Code,
    string Name,
    string Unit,
    string? Notes,
    bool IsActive,
    IReadOnlyList<MaterialStockLineDto> Stocks);

public record GetMaterialsQuery : IRequest<IReadOnlyList<MaterialListItemDto>>;

public class GetMaterialsQueryHandler : IRequestHandler<GetMaterialsQuery, IReadOnlyList<MaterialListItemDto>>
{
    private readonly IAppDbContext _db;
    public GetMaterialsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<MaterialListItemDto>> Handle(GetMaterialsQuery request, CancellationToken cancellationToken)
    {
        var materials = await _db.Materials.AsNoTracking()
            .Include(m => m.StockItems).ThenInclude(s => s.Warehouse)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

        return materials.Select(m => new MaterialListItemDto(
            m.Id,
            m.Code,
            m.Name,
            m.Unit,
            m.Notes,
            m.IsActive,
            m.StockItems
                .Where(s => s.Warehouse.IsActive)
                .OrderBy(s => s.Warehouse.Name)
                .Select(s => new MaterialStockLineDto(s.WarehouseId, s.Warehouse.Name, s.Quantity))
                .ToList()
        )).ToList();
    }
}

public record UpsertMaterialCommand(
    Guid? Id,
    string Code,
    string Name,
    string Unit,
    string? Notes,
    bool IsActive) : IRequest<Guid>;

public class UpsertMaterialCommandValidator : AbstractValidator<UpsertMaterialCommand>
{
    public UpsertMaterialCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class UpsertMaterialCommandHandler : IRequestHandler<UpsertMaterialCommand, Guid>
{
    private readonly IAppDbContext _db;
    public UpsertMaterialCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(UpsertMaterialCommand request, CancellationToken cancellationToken)
    {
        Material material;
        if (request.Id.HasValue)
        {
            material = await _db.Materials.FirstOrDefaultAsync(m => m.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Material not found.");
        }
        else
        {
            material = new Material();
            _db.Materials.Add(material);
        }

        material.Code = request.Code.Trim();
        material.Name = request.Name.Trim();
        material.Unit = request.Unit.Trim();
        material.Notes = request.Notes;
        material.IsActive = request.IsActive;
        material.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return material.Id;
    }
}

public record AdjustMaterialStockCommand(Guid MaterialId, Guid WarehouseId, decimal Quantity) : IRequest;

public class AdjustMaterialStockCommandValidator : AbstractValidator<AdjustMaterialStockCommand>
{
    public AdjustMaterialStockCommandValidator()
    {
        RuleFor(x => x.MaterialId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}

public class AdjustMaterialStockCommandHandler : IRequestHandler<AdjustMaterialStockCommand>
{
    private readonly IAppDbContext _db;
    public AdjustMaterialStockCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(AdjustMaterialStockCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.Materials.AnyAsync(m => m.Id == request.MaterialId, cancellationToken))
            throw new KeyNotFoundException("Material not found.");
        if (!await _db.Warehouses.AnyAsync(w => w.Id == request.WarehouseId, cancellationToken))
            throw new KeyNotFoundException("Warehouse not found.");

        var stock = await _db.MaterialStockItems
            .FirstOrDefaultAsync(s => s.MaterialId == request.MaterialId && s.WarehouseId == request.WarehouseId, cancellationToken);

        if (stock is null)
        {
            _db.MaterialStockItems.Add(new MaterialStockItem
            {
                MaterialId = request.MaterialId,
                WarehouseId = request.WarehouseId,
                Quantity = request.Quantity
            });
        }
        else
        {
            stock.Quantity = request.Quantity;
            stock.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
