using ElsInt.Application.Interfaces;
using ElsInt.Domain.Entities;
using ElsInt.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Application.Admin;

public record BookingMaterialUsageDto(
    Guid Id,
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    string Unit,
    decimal Quantity,
    string? Notes,
    DateTime CreatedAtUtc);

public record GetBookingMaterialsQuery(Guid BookingId) : IRequest<IReadOnlyList<BookingMaterialUsageDto>>;

public class GetBookingMaterialsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetBookingMaterialsQuery, IReadOnlyList<BookingMaterialUsageDto>>
{
    public async Task<IReadOnlyList<BookingMaterialUsageDto>> Handle(
        GetBookingMaterialsQuery request, CancellationToken cancellationToken)
        => await db.BookingMaterialUsages.AsNoTracking()
            .Where(u => u.ServiceBookingId == request.BookingId)
            .OrderBy(u => u.CreatedAtUtc)
            .Select(u => new BookingMaterialUsageDto(
                u.Id, u.MaterialId, u.Material.Code, u.Material.Name, u.Material.Unit,
                u.Quantity, u.Notes, u.CreatedAtUtc))
            .ToListAsync(cancellationToken);
}

public record AddBookingMaterialCommand(
    Guid BookingId,
    Guid MaterialId,
    decimal Quantity,
    string? Notes) : IRequest<Guid>;

public class AddBookingMaterialCommandValidator : AbstractValidator<AddBookingMaterialCommand>
{
    public AddBookingMaterialCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.MaterialId).NotEmpty().WithMessage("Izaberite materijal.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Količina mora biti veća od 0.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class AddBookingMaterialCommandHandler(IAppDbContext db) : IRequestHandler<AddBookingMaterialCommand, Guid>
{
    public async Task<Guid> Handle(AddBookingMaterialCommand request, CancellationToken cancellationToken)
    {
        var booking = await db.ServiceBookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken)
            ?? throw new KeyNotFoundException("Termin nije pronađen.");

        if (booking.Status is not (ServiceBookingStatus.Confirmed or ServiceBookingStatus.Completed))
        {
            throw new InvalidOperationException(
                "Materijal se upisuje samo na potvrđen ili završen termin. " +
                "Prebacite status termina na „Potvrđen“ ili „Završen“ pa pokušajte ponovo.");
        }

        var material = await db.Materials
            .FirstOrDefaultAsync(m => m.Id == request.MaterialId, cancellationToken)
            ?? throw new KeyNotFoundException("Materijal nije pronađen.");

        var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Nema aktivnog magacina.");

        var stock = await db.MaterialStockItems
            .FirstOrDefaultAsync(s => s.MaterialId == material.Id && s.WarehouseId == warehouse.Id, cancellationToken);

        var available = stock?.Quantity ?? 0m;
        if (available < request.Quantity)
        {
            throw new InvalidOperationException(
                $"Nema dovoljno na stanju: {material.Name} — dostupno {available:0.###} {material.Unit}, " +
                $"traženo {request.Quantity:0.###} {material.Unit}. Dopunite zalihe u delu Materijal.");
        }

        stock!.Quantity -= request.Quantity;
        stock.UpdatedAtUtc = DateTime.UtcNow;

        var usage = new BookingMaterialUsage
        {
            ServiceBookingId = booking.Id,
            MaterialId = material.Id,
            WarehouseId = warehouse.Id,
            Quantity = request.Quantity,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };
        db.BookingMaterialUsages.Add(usage);

        booking.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return usage.Id;
    }
}

public record DeleteBookingMaterialCommand(Guid BookingId, Guid UsageId) : IRequest;

public class DeleteBookingMaterialCommandHandler(IAppDbContext db) : IRequestHandler<DeleteBookingMaterialCommand>
{
    public async Task Handle(DeleteBookingMaterialCommand request, CancellationToken cancellationToken)
    {
        var usage = await db.BookingMaterialUsages
            .FirstOrDefaultAsync(u => u.Id == request.UsageId && u.ServiceBookingId == request.BookingId, cancellationToken)
            ?? throw new KeyNotFoundException("Stavka nije pronađena.");

        // Removing a recorded line puts the material back on the shelf.
        var stock = await db.MaterialStockItems
            .FirstOrDefaultAsync(s => s.MaterialId == usage.MaterialId && s.WarehouseId == usage.WarehouseId, cancellationToken);

        if (stock is null)
        {
            db.MaterialStockItems.Add(new MaterialStockItem
            {
                MaterialId = usage.MaterialId,
                WarehouseId = usage.WarehouseId,
                Quantity = usage.Quantity
            });
        }
        else
        {
            stock.Quantity += usage.Quantity;
            stock.UpdatedAtUtc = DateTime.UtcNow;
        }

        db.BookingMaterialUsages.Remove(usage);
        await db.SaveChangesAsync(cancellationToken);
    }
}
