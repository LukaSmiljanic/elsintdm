using ElsInt.Application.Scheduling;
using ElsInt.Domain.Entities;
using ElsInt.Domain.Enums;
using ElsInt.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Application.Tests;

public class AvailableSlotsTests
{
    private static AppDbContext NewDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    [Fact]
    public async Task NoRulesMeansNoSlots()
    {
        await using var db = NewDb();
        var service = new FieldService { Name = "Servis", Slug = "servis", DurationMinutes = 60, IsActive = true };
        db.FieldServices.Add(service);
        await db.SaveChangesAsync();

        var slots = await new GetAvailableSlotsQueryHandler(db)
            .Handle(new GetAvailableSlotsQuery(service.Id, Days: 14), default);

        slots.Should().BeEmpty();
    }

    [Fact]
    public async Task WeekdayRuleOpensSlotsForAllServicesWhenLinksEmpty()
    {
        await using var db = NewDb();
        var service = new FieldService { Name = "Montaža", Slug = "montaza", DurationMinutes = 120, IsActive = true };
        db.FieldServices.Add(service);
        // Pick a Wednesday far enough for lead time.
        var wednesday = Next(DayOfWeek.Wednesday).AddDays(7);
        db.AvailabilityRules.Add(new AvailabilityRule
        {
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(20, 0, 0),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var slots = await new GetAvailableSlotsQueryHandler(db)
            .Handle(new GetAvailableSlotsQuery(service.Id, FromDate: DateOnly.FromDateTime(wednesday), Days: 1), default);

        slots.Should().NotBeEmpty();
        slots.Should().OnlyContain(s => s.StartLocal.StartsWith(wednesday.ToString("yyyy-MM-dd")));
    }

    [Fact]
    public async Task OrphanServiceLinkIsIgnoredSoWindowOpensForEveryone()
    {
        await using var db = NewDb();
        var service = new FieldService { Name = "Servis", Slug = "servis", DurationMinutes = 60, IsActive = true };
        db.FieldServices.Add(service);
        var deadId = Guid.NewGuid();
        var wednesday = Next(DayOfWeek.Wednesday).AddDays(7);
        db.AvailabilityRules.Add(new AvailabilityRule
        {
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(15, 0, 0),
            IsActive = true,
            ServiceLinks = { new AvailabilityRuleService { FieldServiceId = deadId } }
        });
        await db.SaveChangesAsync();

        var slots = await new GetAvailableSlotsQueryHandler(db)
            .Handle(new GetAvailableSlotsQuery(service.Id, FromDate: DateOnly.FromDateTime(wednesday), Days: 1), default);

        slots.Should().NotBeEmpty("dead link must not hide the whole morning window");
    }

    [Fact]
    public async Task ConfirmedBookingRemovesOverlappingSlotOnly()
    {
        await using var db = NewDb();
        var service = new FieldService { Name = "Pranje", Slug = "pranje", DurationMinutes = 30, IsActive = true };
        db.FieldServices.Add(service);
        var wednesday = Next(DayOfWeek.Wednesday).AddDays(7);
        db.AvailabilityRules.Add(new AvailabilityRule
        {
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            IsActive = true
        });
        var busyStart = DateTime.SpecifyKind(wednesday.Date.AddHours(9), DateTimeKind.Unspecified);
        var busyStartUtc = TimeZoneInfo.ConvertTimeToUtc(busyStart, SchedulingDefaults.BelgradeTz);
        db.ServiceBookings.Add(new ServiceBooking
        {
            FieldServiceId = service.Id,
            CustomerName = "Pera",
            CustomerPhone = "064",
            StartUtc = busyStartUtc,
            EndUtc = busyStartUtc.AddMinutes(30),
            Status = ServiceBookingStatus.Confirmed
        });
        await db.SaveChangesAsync();

        var slots = await new GetAvailableSlotsQueryHandler(db)
            .Handle(new GetAvailableSlotsQuery(service.Id, FromDate: DateOnly.FromDateTime(wednesday), Days: 1), default);

        slots.Should().NotBeEmpty();
        slots.Should().NotContain(s => s.StartUtc == busyStartUtc);
        slots.Count.Should().BeGreaterThan(0);
    }

    private static DateTime Next(DayOfWeek day)
    {
        var d = DateTime.Today.AddDays(1);
        while (d.DayOfWeek != day) d = d.AddDays(1);
        return d;
    }
}
