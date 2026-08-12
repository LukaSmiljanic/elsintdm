using ElsInt.Application.Scheduling;
using ElsInt.Domain.Entities;
using ElsInt.Domain.Enums;
using ElsInt.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Application.Tests;

public class ManualBookingTests
{
    private static AppDbContext NewDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static async Task<(AppDbContext db, Guid serviceId)> SeedAsync(int durationMinutes = 120)
    {
        var db = NewDb();
        var service = new FieldService
        {
            Name = "Montaža klime",
            Slug = "montaza-klime",
            DurationMinutes = durationMinutes,
            IsActive = true
        };
        db.FieldServices.Add(service);
        await db.SaveChangesAsync();
        return (db, service.Id);
    }

    private static CreateAdminBookingCommand Command(
        Guid serviceId,
        string startLocal,
        int? duration = null,
        ServiceBookingStatus status = ServiceBookingStatus.Confirmed,
        bool allowOverlap = false)
        => new(serviceId, startLocal, duration, "Pera Perić", "064 123 4567", null, "Novi Sad", null, null,
            status, allowOverlap);

    [Fact]
    public async Task UsesServiceDurationAndConfirmsImmediately()
    {
        var (db, serviceId) = await SeedAsync(90);
        await using var _ = db;

        var dto = await new CreateAdminBookingCommandHandler(db, new SyncEmailDispatcher(new FakeEmailNotifier()))
            .Handle(Command(serviceId, "2026-09-10T09:00"), default);

        dto.Status.Should().Be(ServiceBookingStatus.Confirmed);
        dto.HoldExpiresAtUtc.Should().BeNull();
        dto.StartLocal.Should().Be("2026-09-10 09:00");
        dto.EndLocal.Should().Be("2026-09-10 10:30");
    }

    [Fact]
    public async Task ExplicitDurationOverridesService()
    {
        var (db, serviceId) = await SeedAsync(120);
        await using var _ = db;

        var dto = await new CreateAdminBookingCommandHandler(db, new SyncEmailDispatcher(new FakeEmailNotifier()))
            .Handle(Command(serviceId, "2026-09-10T09:00", duration: 30), default);

        dto.EndLocal.Should().Be("2026-09-10 09:30");
    }

    [Fact]
    public async Task WorksOutsideAvailabilityRulesAndWithoutLeadTime()
    {
        var (db, serviceId) = await SeedAsync(60);
        await using var _ = db;
        // No availability rules at all, and a 22:00 slot an hour from now would fail the public form.
        var soonLocal = DateTime.Now.AddMinutes(30);

        var dto = await new CreateAdminBookingCommandHandler(db, new SyncEmailDispatcher(new FakeEmailNotifier()))
            .Handle(Command(serviceId, soonLocal.ToString("yyyy-MM-ddTHH:mm")), default);

        dto.Id.Should().NotBeEmpty();
        (await db.ServiceBookings.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task OverlappingBookingIsRejectedWithConflictDetails()
    {
        var (db, serviceId) = await SeedAsync(120);
        await using var _ = db;
        var handler = new CreateAdminBookingCommandHandler(db, new SyncEmailDispatcher(new FakeEmailNotifier()));

        await handler.Handle(Command(serviceId, "2026-09-10T09:00"), default);

        var act = () => handler.Handle(Command(serviceId, "2026-09-10T10:00"), default);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*Preklapa se*Montaža klime*10.09.2026 09:00*");
        (await db.ServiceBookings.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AllowOverlapForcesTheBooking()
    {
        var (db, serviceId) = await SeedAsync(120);
        await using var _ = db;
        var handler = new CreateAdminBookingCommandHandler(db, new SyncEmailDispatcher(new FakeEmailNotifier()));

        await handler.Handle(Command(serviceId, "2026-09-10T09:00"), default);
        await handler.Handle(Command(serviceId, "2026-09-10T10:00", allowOverlap: true), default);

        (await db.ServiceBookings.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task BackToBackBookingIsAllowed()
    {
        var (db, serviceId) = await SeedAsync(120);
        await using var _ = db;
        var handler = new CreateAdminBookingCommandHandler(db, new SyncEmailDispatcher(new FakeEmailNotifier()));

        await handler.Handle(Command(serviceId, "2026-09-10T09:00"), default);
        await handler.Handle(Command(serviceId, "2026-09-10T11:00"), default);

        (await db.ServiceBookings.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task CancelledBookingDoesNotBlockTheSlot()
    {
        var (db, serviceId) = await SeedAsync(120);
        await using var _ = db;
        var handler = new CreateAdminBookingCommandHandler(db, new SyncEmailDispatcher(new FakeEmailNotifier()));

        var first = await handler.Handle(Command(serviceId, "2026-09-10T09:00"), default);
        var booking = await db.ServiceBookings.FirstAsync(b => b.Id == first.Id);
        booking.Status = ServiceBookingStatus.Cancelled;
        await db.SaveChangesAsync();

        await handler.Handle(Command(serviceId, "2026-09-10T09:00"), default);

        (await db.ServiceBookings.CountAsync(b => b.Status == ServiceBookingStatus.Confirmed)).Should().Be(1);
    }

    [Fact]
    public async Task CompletedBookingStillBlocksTheSlot()
    {
        var (db, serviceId) = await SeedAsync(120);
        await using var _ = db;
        var handler = new CreateAdminBookingCommandHandler(db, new SyncEmailDispatcher(new FakeEmailNotifier()));

        await handler.Handle(Command(serviceId, "2026-09-10T09:00", status: ServiceBookingStatus.Completed), default);

        var act = () => handler.Handle(Command(serviceId, "2026-09-10T09:30"), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task InvalidDateIsRejected()
    {
        var (db, serviceId) = await SeedAsync();
        await using var _ = db;

        var act = () => new CreateAdminBookingCommandHandler(db, new SyncEmailDispatcher(new FakeEmailNotifier()))
            .Handle(Command(serviceId, "not-a-date"), default);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*Neispravan datum*");
    }

    [Fact]
    public async Task MissingServiceIsRejected()
    {
        var (db, _) = await SeedAsync();
        await using var __ = db;

        var act = () => new CreateAdminBookingCommandHandler(db, new SyncEmailDispatcher(new FakeEmailNotifier()))
            .Handle(Command(Guid.NewGuid(), "2026-09-10T09:00"), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
