using ElsInt.Application.Admin;
using ElsInt.Domain.Entities;
using ElsInt.Domain.Enums;
using ElsInt.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Application.Tests;

public class BookingMaterialUsageTests
{
    private static async Task<(AppDbContext db, Guid bookingId, Guid materialId, Guid warehouseId)> SeedAsync(
        decimal stockOnHand,
        ServiceBookingStatus status = ServiceBookingStatus.Completed)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var warehouse = new Warehouse { Name = "Novi Sad", Code = "NS-01", IsActive = true };
        var material = new Material { Code = "CEV-6", Name = "Bakarna cev 1/4", Unit = "m" };
        var service = new FieldService { Name = "Montaža", Slug = "montaza", DurationMinutes = 120 };
        db.Warehouses.Add(warehouse);
        db.Materials.Add(material);
        db.FieldServices.Add(service);
        await db.SaveChangesAsync();

        db.MaterialStockItems.Add(new MaterialStockItem
        {
            MaterialId = material.Id,
            WarehouseId = warehouse.Id,
            Quantity = stockOnHand
        });

        var booking = new ServiceBooking
        {
            FieldServiceId = service.Id,
            CustomerName = "Pera",
            CustomerPhone = "0671234567",
            StartUtc = DateTime.UtcNow.AddDays(1),
            EndUtc = DateTime.UtcNow.AddDays(1).AddHours(2),
            Status = status
        };
        db.ServiceBookings.Add(booking);
        await db.SaveChangesAsync();

        return (db, booking.Id, material.Id, warehouse.Id);
    }

    private static async Task<decimal> OnHandAsync(AppDbContext db, Guid materialId, Guid warehouseId)
        => (await db.MaterialStockItems.AsNoTracking()
            .FirstAsync(s => s.MaterialId == materialId && s.WarehouseId == warehouseId)).Quantity;

    [Fact]
    public async Task RecordingUsageDeductsStock()
    {
        var (db, bookingId, materialId, warehouseId) = await SeedAsync(10m);
        await using var _ = db;

        await new AddBookingMaterialCommandHandler(db)
            .Handle(new AddBookingMaterialCommand(bookingId, materialId, 2.5m, "2 spoja"), default);

        (await OnHandAsync(db, materialId, warehouseId)).Should().Be(7.5m);
        var lines = await db.BookingMaterialUsages.AsNoTracking().ToListAsync();
        lines.Should().ContainSingle();
        lines[0].Quantity.Should().Be(2.5m);
        lines[0].Notes.Should().Be("2 spoja");
    }

    [Fact]
    public async Task MultipleLinesAccumulate()
    {
        var (db, bookingId, materialId, warehouseId) = await SeedAsync(10m);
        await using var _ = db;
        var handler = new AddBookingMaterialCommandHandler(db);

        await handler.Handle(new AddBookingMaterialCommand(bookingId, materialId, 3m, null), default);
        await handler.Handle(new AddBookingMaterialCommand(bookingId, materialId, 1.5m, null), default);

        (await OnHandAsync(db, materialId, warehouseId)).Should().Be(5.5m);
        (await db.BookingMaterialUsages.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task DeletingUsageReturnsStock()
    {
        var (db, bookingId, materialId, warehouseId) = await SeedAsync(10m);
        await using var _ = db;

        var usageId = await new AddBookingMaterialCommandHandler(db)
            .Handle(new AddBookingMaterialCommand(bookingId, materialId, 4m, null), default);

        await new DeleteBookingMaterialCommandHandler(db)
            .Handle(new DeleteBookingMaterialCommand(bookingId, usageId), default);

        (await OnHandAsync(db, materialId, warehouseId)).Should().Be(10m);
        (await db.BookingMaterialUsages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ConfirmedBookingAcceptsUsage()
    {
        var (db, bookingId, materialId, warehouseId) = await SeedAsync(10m, ServiceBookingStatus.Confirmed);
        await using var _ = db;

        await new AddBookingMaterialCommandHandler(db)
            .Handle(new AddBookingMaterialCommand(bookingId, materialId, 1m, null), default);

        (await OnHandAsync(db, materialId, warehouseId)).Should().Be(9m);
    }

    [Theory]
    [InlineData(ServiceBookingStatus.Held)]
    [InlineData(ServiceBookingStatus.Cancelled)]
    [InlineData(ServiceBookingStatus.Expired)]
    public async Task UsageIsRejectedUnlessBookingIsConfirmedOrCompleted(ServiceBookingStatus status)
    {
        var (db, bookingId, materialId, warehouseId) = await SeedAsync(10m, status);
        await using var _ = db;

        var act = () => new AddBookingMaterialCommandHandler(db)
            .Handle(new AddBookingMaterialCommand(bookingId, materialId, 1m, null), default);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*potvrđen ili završen termin*");
        (await OnHandAsync(db, materialId, warehouseId)).Should().Be(10m);
        (await db.BookingMaterialUsages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CancelledBookingStillAllowsCorrectingRecordedUsage()
    {
        var (db, bookingId, materialId, warehouseId) = await SeedAsync(10m);
        await using var _ = db;

        var usageId = await new AddBookingMaterialCommandHandler(db)
            .Handle(new AddBookingMaterialCommand(bookingId, materialId, 4m, null), default);

        var booking = await db.ServiceBookings.FirstAsync(b => b.Id == bookingId);
        booking.Status = ServiceBookingStatus.Cancelled;
        await db.SaveChangesAsync();

        await new DeleteBookingMaterialCommandHandler(db)
            .Handle(new DeleteBookingMaterialCommand(bookingId, usageId), default);

        (await OnHandAsync(db, materialId, warehouseId)).Should().Be(10m);
    }

    [Fact]
    public async Task CannotSpendMoreThanOnHand()
    {
        var (db, bookingId, materialId, warehouseId) = await SeedAsync(2m);
        await using var _ = db;

        var act = () => new AddBookingMaterialCommandHandler(db)
            .Handle(new AddBookingMaterialCommand(bookingId, materialId, 5m, null), default);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*dostupno 2*");
        (await OnHandAsync(db, materialId, warehouseId)).Should().Be(2m);
        (await db.BookingMaterialUsages.CountAsync()).Should().Be(0);
    }
}
