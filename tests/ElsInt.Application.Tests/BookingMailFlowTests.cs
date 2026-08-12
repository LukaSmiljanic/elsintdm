using ElsInt.Application.Scheduling;
using ElsInt.Domain.Entities;
using ElsInt.Domain.Enums;
using ElsInt.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Application.Tests;

public class BookingMailFlowTests
{
    private static AppDbContext NewDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    [Fact]
    public async Task AdminConfirmedBookingEmailsAdminAndCustomer()
    {
        await using var db = NewDb();
        var service = new FieldService { Name = "Servis", Slug = "servis", DurationMinutes = 60, IsActive = true };
        db.FieldServices.Add(service);
        await db.SaveChangesAsync();

        var email = new FakeEmailNotifier();
        var dispatch = new SyncEmailDispatcher(email);
        await new CreateAdminBookingCommandHandler(db, dispatch).Handle(
            new CreateAdminBookingCommand(
                service.Id, "2026-09-10T10:00", null, "Ana Anić", "064111222",
                "ana@test.rs", "NS", null, null, ServiceBookingStatus.Confirmed),
            default);

        email.Sent.Should().Contain(x => x.To == "__notify__" && x.Subject.Contains("potvrđen"));
        email.Sent.Should().Contain(x => x.To == "ana@test.rs" && x.Subject.Contains("potvrđen"));
    }

    [Fact]
    public async Task ConfirmingHoldEmailsCustomerOnly()
    {
        await using var db = NewDb();
        var service = new FieldService { Name = "Pranje", Slug = "pranje", DurationMinutes = 30, IsActive = true };
        db.FieldServices.Add(service);
        var booking = new ServiceBooking
        {
            FieldServiceId = service.Id,
            CustomerName = "Pera",
            CustomerPhone = "064",
            CustomerEmail = "pera@test.rs",
            StartUtc = DateTime.UtcNow.AddDays(2),
            EndUtc = DateTime.UtcNow.AddDays(2).AddMinutes(30),
            Status = ServiceBookingStatus.Held,
            HoldExpiresAtUtc = DateTime.UtcNow.AddHours(2)
        };
        db.ServiceBookings.Add(booking);
        await db.SaveChangesAsync();

        var email = new FakeEmailNotifier();
        var dispatch = new SyncEmailDispatcher(email);
        await new UpdateBookingStatusCommandHandler(db, dispatch)
            .Handle(new UpdateBookingStatusCommand(booking.Id, ServiceBookingStatus.Confirmed, null), default);

        email.Sent.Should().ContainSingle(x => x.To == "pera@test.rs");
        email.Sent.Should().Contain(x => x.To == "__notify__" && x.Subject.Contains("→ Potvrđen"));
    }

    [Fact]
    public async Task CancellingEmailsCustomer()
    {
        await using var db = NewDb();
        var service = new FieldService { Name = "Pranje", Slug = "pranje", DurationMinutes = 30, IsActive = true };
        db.FieldServices.Add(service);
        var booking = new ServiceBooking
        {
            FieldServiceId = service.Id,
            CustomerName = "Pera",
            CustomerPhone = "064",
            CustomerEmail = "pera@test.rs",
            StartUtc = DateTime.UtcNow.AddDays(2),
            EndUtc = DateTime.UtcNow.AddDays(2).AddMinutes(30),
            Status = ServiceBookingStatus.Confirmed
        };
        db.ServiceBookings.Add(booking);
        await db.SaveChangesAsync();

        var email = new FakeEmailNotifier();
        var dispatch = new SyncEmailDispatcher(email);
        await new UpdateBookingStatusCommandHandler(db, dispatch)
            .Handle(new UpdateBookingStatusCommand(booking.Id, ServiceBookingStatus.Cancelled, null), default);

        email.Sent.Should().ContainSingle(x => x.To == "pera@test.rs" && x.Subject.Contains("otkazan"));
        email.Sent.Should().Contain(x => x.To == "__notify__" && x.Subject.Contains("→ Otkazan"));
    }
}
