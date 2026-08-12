using System.Globalization;
using ElsInt.Application.Interfaces;
using ElsInt.Domain.Entities;
using ElsInt.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ElsInt.Application.Scheduling;

public static class SchedulingDefaults
{
    public const int MinLeadHours = 4;
    public const int HoldHours = 4;
    public const int HorizonDays = 21;
    /// <summary>Single field team: any Held/Confirmed booking blocks that time for all services.</summary>
    public const int TeamCapacity = 1;
    public const string TimeZoneId = "Europe/Belgrade";

    public static TimeZoneInfo BelgradeTz
    {
        get
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId); }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            }
        }
    }
}

public record FieldServiceDto(
    Guid Id, string Name, string Slug, string? Description, int DurationMinutes, decimal Price, bool IsActive, int SortOrder);

public record AvailabilityRuleDto(
    Guid Id, DayOfWeek DayOfWeek, string StartTime, string EndTime, bool IsActive, string? Label, IReadOnlyList<Guid> ServiceIds);

public record ServiceSlotDto(DateTime StartUtc, DateTime EndUtc, string StartLocal, string EndLocal);

public record ServiceBookingDto(
    Guid Id, Guid FieldServiceId, string ServiceName, string CustomerName, string CustomerPhone,
    string? CustomerEmail, string? Address, string? Notes, DateTime StartUtc, DateTime EndUtc,
    string StartLocal, string EndLocal, ServiceBookingStatus Status, DateTime? HoldExpiresAtUtc, string? AdminNotes,
    DateTime CreatedAtUtc, int MaterialLines = 0);

public record GetPublicServicesQuery : IRequest<IReadOnlyList<FieldServiceDto>>;

public class GetPublicServicesQueryHandler : IRequestHandler<GetPublicServicesQuery, IReadOnlyList<FieldServiceDto>>
{
    private readonly IAppDbContext _db;
    public GetPublicServicesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<FieldServiceDto>> Handle(GetPublicServicesQuery request, CancellationToken cancellationToken)
        => await _db.FieldServices.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
            .Select(s => new FieldServiceDto(s.Id, s.Name, s.Slug, s.Description, s.DurationMinutes, s.Price, s.IsActive, s.SortOrder))
            .ToListAsync(cancellationToken);
}

public record GetAvailableSlotsQuery(Guid ServiceId, DateOnly? FromDate = null, int Days = SchedulingDefaults.HorizonDays)
    : IRequest<IReadOnlyList<ServiceSlotDto>>;

public class GetAvailableSlotsQueryHandler : IRequestHandler<GetAvailableSlotsQuery, IReadOnlyList<ServiceSlotDto>>
{
    private readonly IAppDbContext _db;
    public GetAvailableSlotsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ServiceSlotDto>> Handle(GetAvailableSlotsQuery request, CancellationToken cancellationToken)
    {
        await BookingHelpers.ExpireHoldsAsync(_db, cancellationToken);

        var service = await _db.FieldServices.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Usluga nije pronađena.");

        var tz = SchedulingDefaults.BelgradeTz;
        var nowUtc = DateTime.UtcNow;
        var earliestUtc = nowUtc.AddHours(SchedulingDefaults.MinLeadHours);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
        var fromLocal = request.FromDate?.ToDateTime(TimeOnly.MinValue) ?? localNow.Date;
        if (fromLocal < localNow.Date) fromLocal = localNow.Date;

        var days = Math.Clamp(request.Days, 1, 45);
        var rangeEndLocal = fromLocal.Date.AddDays(days);
        var rangeStartUtc = TimeZoneInfo.ConvertTimeToUtc(fromLocal.Date, tz);
        var rangeEndUtc = TimeZoneInfo.ConvertTimeToUtc(rangeEndLocal, tz);

        var rules = await _db.AvailabilityRules.AsNoTracking()
            .Include(r => r.ServiceLinks)
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        // Links to deleted services would otherwise hide whole windows forever.
        var liveServiceIds = await _db.FieldServices.AsNoTracking().Select(s => s.Id).ToHashSetAsync(cancellationToken);
        foreach (var rule in rules)
        {
            var dead = rule.ServiceLinks.Where(l => !liveServiceIds.Contains(l.FieldServiceId)).ToList();
            foreach (var link in dead)
                rule.ServiceLinks.Remove(link);
        }

        // Busy across ALL services — one team cannot do montaža and pranje at the same time.
        var busy = await BookingHelpers.GetBusyIntervalsAsync(_db, rangeStartUtc, rangeEndUtc, cancellationToken);

        var slots = new List<ServiceSlotDto>();
        var duration = TimeSpan.FromMinutes(Math.Max(15, service.DurationMinutes));
        var step = duration;

        for (var day = 0; day < days; day++)
        {
            var date = DateOnly.FromDateTime(fromLocal.AddDays(day));
            var dow = date.DayOfWeek;
            var dayRules = rules.Where(r => r.DayOfWeek == dow).ToList();
            foreach (var rule in dayRules)
            {
                var allowsAll = rule.ServiceLinks.Count == 0;
                var allowsService = allowsAll || rule.ServiceLinks.Any(l => l.FieldServiceId == service.Id);
                if (!allowsService) continue;
                if (rule.EndTime <= rule.StartTime) continue;

                var cursorLocal = date.ToDateTime(TimeOnly.FromTimeSpan(rule.StartTime));
                var windowEndLocal = date.ToDateTime(TimeOnly.FromTimeSpan(rule.EndTime));

                while (cursorLocal + duration <= windowEndLocal)
                {
                    var slotStartLocal = cursorLocal;
                    var slotEndLocal = cursorLocal + duration;
                    var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(slotStartLocal, DateTimeKind.Unspecified), tz);
                    var endUtc = startUtc + duration;
                    cursorLocal += step;

                    if (startUtc < earliestUtc) continue;
                    if (BookingHelpers.OverlapsAny(busy, startUtc, endUtc)) continue;

                    slots.Add(new ServiceSlotDto(
                        startUtc,
                        endUtc,
                        slotStartLocal.ToString("yyyy-MM-dd HH:mm"),
                        slotEndLocal.ToString("yyyy-MM-dd HH:mm")));
                }
            }
        }

        return slots.OrderBy(s => s.StartUtc).ToList();
    }
}

public record CreateServiceBookingCommand(
    Guid ServiceId,
    DateTime StartUtc,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    string? Address,
    string? Notes) : IRequest<ServiceBookingDto>;

public class CreateServiceBookingCommandValidator : AbstractValidator<CreateServiceBookingCommand>
{
    public CreateServiceBookingCommandValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CustomerPhone).NotEmpty().MaximumLength(40);
        RuleFor(x => x.CustomerEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail));
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class CreateServiceBookingCommandHandler : IRequestHandler<CreateServiceBookingCommand, ServiceBookingDto>
{
    private readonly IAppDbContext _db;
    private readonly IBackgroundEmailDispatcher _mailDispatch;
    private readonly IConfiguration _config;

    public CreateServiceBookingCommandHandler(
        IAppDbContext db, IBackgroundEmailDispatcher mailDispatch, IConfiguration config)
    {
        _db = db;
        _mailDispatch = mailDispatch;
        _config = config;
    }

    public async Task<ServiceBookingDto> Handle(CreateServiceBookingCommand request, CancellationToken cancellationToken)
    {
        await BookingHelpers.ExpireHoldsAsync(_db, cancellationToken);

        var service = await _db.FieldServices
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Usluga nije pronađena.");

        var tz = SchedulingDefaults.BelgradeTz;
        var nowUtc = DateTime.UtcNow;
        var earliestUtc = nowUtc.AddHours(SchedulingDefaults.MinLeadHours);
        var startUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc);
        var endUtc = startUtc.AddMinutes(service.DurationMinutes);

        if (startUtc < earliestUtc)
            throw new InvalidOperationException($"Termin mora biti najmanje {SchedulingDefaults.MinLeadHours}h unapred.");

        var localStart = TimeZoneInfo.ConvertTimeFromUtc(startUtc, tz);
        var rules = await _db.AvailabilityRules.AsNoTracking()
            .Include(r => r.ServiceLinks)
            .Where(r => r.IsActive && r.DayOfWeek == localStart.DayOfWeek)
            .ToListAsync(cancellationToken);

        var allowed = rules.Any(r =>
        {
            var serviceOk = r.ServiceLinks.Count == 0 || r.ServiceLinks.Any(l => l.FieldServiceId == service.Id);
            if (!serviceOk) return false;
            var windowStart = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localStart.Date.Add(r.StartTime), DateTimeKind.Unspecified), tz);
            var windowEnd = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localStart.Date.Add(r.EndTime), DateTimeKind.Unspecified), tz);
            return startUtc >= windowStart && endUtc <= windowEnd;
        });

        if (!allowed)
            throw new InvalidOperationException("Izabrani termin nije u dostupnom rasporedu za ovu uslugu.");

        var busy = await BookingHelpers.GetBusyIntervalsAsync(_db, startUtc, endUtc, cancellationToken);
        if (BookingHelpers.OverlapsAny(busy, startUtc, endUtc))
            throw new InvalidOperationException("Termin je zauzet (ekipa je već na drugom poslu). Izaberite drugi slot.");

        var booking = new ServiceBooking
        {
            FieldServiceId = service.Id,
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            CustomerEmail = string.IsNullOrWhiteSpace(request.CustomerEmail) ? null : request.CustomerEmail.Trim(),
            Address = request.Address,
            Notes = request.Notes,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Status = ServiceBookingStatus.Held,
            HoldExpiresAtUtc = nowUtc.AddHours(SchedulingDefaults.HoldHours)
        };
        _db.ServiceBookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);

        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(endUtc, tz);
        var dto = new ServiceBookingDto(
            booking.Id, service.Id, service.Name, booking.CustomerName, booking.CustomerPhone,
            booking.CustomerEmail, booking.Address, booking.Notes, booking.StartUtc, booking.EndUtc,
            localStart.ToString("yyyy-MM-dd HH:mm"), localEnd.ToString("yyyy-MM-dd HH:mm"),
            booking.Status, booking.HoldExpiresAtUtc, booking.AdminNotes, booking.CreatedAtUtc);

        var adminUrl = (_config["Storefront:BaseUrl"] ?? "https://elsintdm.rs").TrimEnd('/') + "/admin/bookings";
        _mailDispatch.Enqueue((email, ct) => BookingMail.NotifyNewPublicHoldAsync(
            email, service.Name, booking.CustomerName, booking.CustomerPhone, booking.CustomerEmail,
            booking.Address, booking.Notes, dto.StartLocal, dto.EndLocal, booking.HoldExpiresAtUtc,
            adminUrl, ct));

        return dto;
    }
}

internal static class BookingHelpers
{
    public static async Task ExpireHoldsAsync(IAppDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expired = await db.ServiceBookings
            .Where(b => b.Status == ServiceBookingStatus.Held && b.HoldExpiresAtUtc != null && b.HoldExpiresAtUtc <= now)
            .ToListAsync(cancellationToken);
        if (expired.Count == 0) return;
        foreach (var b in expired)
        {
            b.Status = ServiceBookingStatus.Expired;
            b.UpdatedAtUtc = now;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task<IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)>> GetBusyIntervalsAsync(
        IAppDbContext db, DateTime rangeStartUtc, DateTime rangeEndUtc, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        // Intentionally NOT filtered by service — one team blocks the whole calendar.
        var rows = await db.ServiceBookings.AsNoTracking()
            .Where(b =>
                b.StartUtc < rangeEndUtc &&
                b.EndUtc > rangeStartUtc &&
                (b.Status == ServiceBookingStatus.Confirmed ||
                 (b.Status == ServiceBookingStatus.Held && b.HoldExpiresAtUtc != null && b.HoldExpiresAtUtc > nowUtc)))
            .Select(b => new { b.StartUtc, b.EndUtc })
            .ToListAsync(cancellationToken);
        return rows.Select(b => (b.StartUtc, b.EndUtc)).ToList();
    }

    public static bool OverlapsAny(IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> busy, DateTime startUtc, DateTime endUtc)
        => busy.Any(b => b.StartUtc < endUtc && b.EndUtc > startUtc);
}

// ---- Admin ----

public record GetAdminServicesQuery : IRequest<IReadOnlyList<FieldServiceDto>>;
public class GetAdminServicesQueryHandler(IAppDbContext db) : IRequestHandler<GetAdminServicesQuery, IReadOnlyList<FieldServiceDto>>
{
    public async Task<IReadOnlyList<FieldServiceDto>> Handle(GetAdminServicesQuery request, CancellationToken cancellationToken)
        => await db.FieldServices.AsNoTracking()
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
            .Select(s => new FieldServiceDto(s.Id, s.Name, s.Slug, s.Description, s.DurationMinutes, s.Price, s.IsActive, s.SortOrder))
            .ToListAsync(cancellationToken);
}

public record UpsertFieldServiceCommand(
    Guid? Id, string Name, string Slug, string? Description, int DurationMinutes, decimal Price, bool IsActive, int SortOrder)
    : IRequest<Guid>;

public class UpsertFieldServiceCommandValidator : AbstractValidator<UpsertFieldServiceCommand>
{
    public UpsertFieldServiceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(180);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(30, 480);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public class UpsertFieldServiceCommandHandler(IAppDbContext db) : IRequestHandler<UpsertFieldServiceCommand, Guid>
{
    public async Task<Guid> Handle(UpsertFieldServiceCommand request, CancellationToken cancellationToken)
    {
        FieldService entity;
        if (request.Id.HasValue)
        {
            entity = await db.FieldServices.FirstOrDefaultAsync(s => s.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Service not found.");
        }
        else
        {
            entity = new FieldService();
            db.FieldServices.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.Slug = request.Slug.Trim();
        entity.Description = request.Description;
        entity.DurationMinutes = request.DurationMinutes;
        entity.Price = request.Price;
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public record GetAvailabilityRulesQuery : IRequest<IReadOnlyList<AvailabilityRuleDto>>;
public class GetAvailabilityRulesQueryHandler(IAppDbContext db) : IRequestHandler<GetAvailabilityRulesQuery, IReadOnlyList<AvailabilityRuleDto>>
{
    public async Task<IReadOnlyList<AvailabilityRuleDto>> Handle(GetAvailabilityRulesQuery request, CancellationToken cancellationToken)
    {
        var rules = await db.AvailabilityRules.AsNoTracking()
            .Include(r => r.ServiceLinks)
            .OrderBy(r => r.DayOfWeek).ThenBy(r => r.StartTime)
            .ToListAsync(cancellationToken);
        return rules.Select(r => new AvailabilityRuleDto(
            r.Id, r.DayOfWeek,
            r.StartTime.ToString(@"hh\:mm"), r.EndTime.ToString(@"hh\:mm"),
            r.IsActive, r.Label,
            r.ServiceLinks.Select(l => l.FieldServiceId).ToList())).ToList();
    }
}

public record UpsertAvailabilityRuleCommand(
    Guid? Id, DayOfWeek DayOfWeek, string StartTime, string EndTime, bool IsActive, string? Label, IReadOnlyList<Guid> ServiceIds)
    : IRequest<Guid>;

public class UpsertAvailabilityRuleCommandValidator : AbstractValidator<UpsertAvailabilityRuleCommand>
{
    public UpsertAvailabilityRuleCommandValidator()
    {
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.EndTime).NotEmpty();
    }
}

public class UpsertAvailabilityRuleCommandHandler(IAppDbContext db) : IRequestHandler<UpsertAvailabilityRuleCommand, Guid>
{
    public async Task<Guid> Handle(UpsertAvailabilityRuleCommand request, CancellationToken cancellationToken)
    {
        if (!TimeSpan.TryParse(request.StartTime, out var start) || !TimeSpan.TryParse(request.EndTime, out var end) || end <= start)
            throw new InvalidOperationException("Neispravan period (Start/End).");

        AvailabilityRule rule;
        if (request.Id.HasValue)
        {
            rule = await db.AvailabilityRules.Include(r => r.ServiceLinks)
                .FirstOrDefaultAsync(r => r.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Rule not found.");
            db.AvailabilityRuleServices.RemoveRange(rule.ServiceLinks);
        }
        else
        {
            rule = new AvailabilityRule();
            db.AvailabilityRules.Add(rule);
        }

        rule.DayOfWeek = request.DayOfWeek;
        rule.StartTime = start;
        rule.EndTime = end;
        rule.IsActive = request.IsActive;
        rule.Label = request.Label;
        rule.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var sid in request.ServiceIds.Distinct())
            rule.ServiceLinks.Add(new AvailabilityRuleService { FieldServiceId = sid });

        await db.SaveChangesAsync(cancellationToken);
        return rule.Id;
    }
}

public record DeleteAvailabilityRuleCommand(Guid Id) : IRequest;
public class DeleteAvailabilityRuleCommandHandler(IAppDbContext db) : IRequestHandler<DeleteAvailabilityRuleCommand>
{
    public async Task Handle(DeleteAvailabilityRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await db.AvailabilityRules.Include(r => r.ServiceLinks)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Rule not found.");
        db.AvailabilityRuleServices.RemoveRange(rule.ServiceLinks);
        db.AvailabilityRules.Remove(rule);
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Booking taken over the phone. Unlike the public form this skips the availability rules and the
/// minimum lead time (the call already settled those), but it still refuses to double-book the team.
/// </summary>
public record CreateAdminBookingCommand(
    Guid ServiceId,
    string StartLocal,
    int? DurationMinutes,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    string? Address,
    string? Notes,
    string? AdminNotes,
    ServiceBookingStatus Status = ServiceBookingStatus.Confirmed,
    bool AllowOverlap = false) : IRequest<ServiceBookingDto>;

public class CreateAdminBookingCommandValidator : AbstractValidator<CreateAdminBookingCommand>
{
    public CreateAdminBookingCommandValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty().WithMessage("Izaberite uslugu.");
        RuleFor(x => x.StartLocal).NotEmpty().WithMessage("Unesite datum i vreme.");
        RuleFor(x => x.CustomerName).NotEmpty().WithMessage("Unesite ime klijenta.").MaximumLength(150);
        RuleFor(x => x.CustomerPhone).NotEmpty().WithMessage("Unesite telefon.").MaximumLength(40);
        RuleFor(x => x.CustomerEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail));
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.AdminNotes).MaximumLength(1000);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 600)
            .When(x => x.DurationMinutes.HasValue)
            .WithMessage("Trajanje mora biti između 15 i 600 minuta.");
        RuleFor(x => x.Status)
            .Must(s => s is ServiceBookingStatus.Confirmed or ServiceBookingStatus.Completed)
            .WithMessage("Ručni termin može biti samo potvrđen ili završen.");
    }
}

public class CreateAdminBookingCommandHandler(IAppDbContext db, IBackgroundEmailDispatcher mailDispatch)
    : IRequestHandler<CreateAdminBookingCommand, ServiceBookingDto>
{
    public async Task<ServiceBookingDto> Handle(CreateAdminBookingCommand request, CancellationToken cancellationToken)
    {
        await BookingHelpers.ExpireHoldsAsync(db, cancellationToken);

        var service = await db.FieldServices
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Usluga nije pronađena.");

        var tz = SchedulingDefaults.BelgradeTz;

        if (!DateTime.TryParse(
                request.StartLocal, CultureInfo.InvariantCulture, DateTimeStyles.None, out var localStart))
            throw new InvalidOperationException("Neispravan datum ili vreme termina.");

        localStart = DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified);
        if (tz.IsInvalidTime(localStart))
            throw new InvalidOperationException("To vreme ne postoji zbog prelaska na letnje računanje vremena.");

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
        var duration = request.DurationMinutes ?? service.DurationMinutes;
        var endUtc = startUtc.AddMinutes(duration);

        if (!request.AllowOverlap)
        {
            var conflict = await db.ServiceBookings.AsNoTracking()
                .Include(b => b.FieldService)
                .Where(b =>
                    b.StartUtc < endUtc &&
                    b.EndUtc > startUtc &&
                    (b.Status == ServiceBookingStatus.Confirmed ||
                     b.Status == ServiceBookingStatus.Completed ||
                     (b.Status == ServiceBookingStatus.Held &&
                      b.HoldExpiresAtUtc != null &&
                      b.HoldExpiresAtUtc > DateTime.UtcNow)))
                .OrderBy(b => b.StartUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (conflict is not null)
            {
                var conflictStart = TimeZoneInfo.ConvertTimeFromUtc(conflict.StartUtc, tz);
                var conflictEnd = TimeZoneInfo.ConvertTimeFromUtc(conflict.EndUtc, tz);
                throw new InvalidOperationException(
                    $"Preklapa se sa terminom: {conflict.FieldService.Name}, " +
                    $"{conflictStart:dd.MM.yyyy HH:mm}–{conflictEnd:HH:mm}, {conflict.CustomerName}. " +
                    "Izaberite drugo vreme ili označite „dozvoli preklapanje“.");
            }
        }

        var booking = new ServiceBooking
        {
            FieldServiceId = service.Id,
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            CustomerEmail = string.IsNullOrWhiteSpace(request.CustomerEmail) ? null : request.CustomerEmail.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            AdminNotes = string.IsNullOrWhiteSpace(request.AdminNotes) ? null : request.AdminNotes.Trim(),
            StartUtc = startUtc,
            EndUtc = endUtc,
            Status = request.Status,
            HoldExpiresAtUtc = null
        };
        db.ServiceBookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);

        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(endUtc, tz);
        var startLocal = localStart.ToString("yyyy-MM-dd HH:mm");
        var endLocal = localEnd.ToString("yyyy-MM-dd HH:mm");

        if (request.Status == ServiceBookingStatus.Confirmed)
        {
            mailDispatch.Enqueue((em, ct) => BookingMail.NotifyConfirmedAsync(
                em, service.Name, booking.CustomerName, booking.CustomerPhone, booking.CustomerEmail,
                booking.Address, startLocal, endLocal, notifyAdmin: true,
                adminSource: "Ručni unos (telefon)", ct));
        }

        return new ServiceBookingDto(
            booking.Id, service.Id, service.Name, booking.CustomerName, booking.CustomerPhone,
            booking.CustomerEmail, booking.Address, booking.Notes, booking.StartUtc, booking.EndUtc,
            startLocal, endLocal,
            booking.Status, booking.HoldExpiresAtUtc, booking.AdminNotes, booking.CreatedAtUtc);
    }
}

public record GetAdminBookingsQuery(ServiceBookingStatus? Status = null, DateOnly? From = null, DateOnly? To = null)
    : IRequest<IReadOnlyList<ServiceBookingDto>>;

public class GetAdminBookingsQueryHandler(IAppDbContext db) : IRequestHandler<GetAdminBookingsQuery, IReadOnlyList<ServiceBookingDto>>
{
    public async Task<IReadOnlyList<ServiceBookingDto>> Handle(GetAdminBookingsQuery request, CancellationToken cancellationToken)
    {
        await BookingHelpers.ExpireHoldsAsync(db, cancellationToken);
        var tz = SchedulingDefaults.BelgradeTz;
        var query = db.ServiceBookings.AsNoTracking()
            .Include(b => b.FieldService)
            .Include(b => b.MaterialUsages)
            .AsQueryable();
        if (request.Status.HasValue)
            query = query.Where(b => b.Status == request.Status.Value);
        if (request.From.HasValue)
        {
            var fromUtc = TimeZoneInfo.ConvertTimeToUtc(request.From.Value.ToDateTime(TimeOnly.MinValue), tz);
            query = query.Where(b => b.EndUtc >= fromUtc);
        }
        if (request.To.HasValue)
        {
            var toUtc = TimeZoneInfo.ConvertTimeToUtc(request.To.Value.ToDateTime(TimeOnly.MaxValue), tz);
            query = query.Where(b => b.StartUtc <= toUtc);
        }

        var items = await query.OrderBy(b => b.StartUtc).ToListAsync(cancellationToken);
        return items.Select(b =>
        {
            var startLocal = TimeZoneInfo.ConvertTimeFromUtc(b.StartUtc, tz);
            var endLocal = TimeZoneInfo.ConvertTimeFromUtc(b.EndUtc, tz);
            return new ServiceBookingDto(
                b.Id, b.FieldServiceId, b.FieldService.Name, b.CustomerName, b.CustomerPhone, b.CustomerEmail,
                b.Address, b.Notes, b.StartUtc, b.EndUtc,
                startLocal.ToString("yyyy-MM-dd HH:mm"), endLocal.ToString("yyyy-MM-dd HH:mm"),
                b.Status, b.HoldExpiresAtUtc, b.AdminNotes, b.CreatedAtUtc, b.MaterialUsages.Count);
        }).ToList();
    }
}

public record UpdateBookingStatusCommand(Guid Id, ServiceBookingStatus Status, string? AdminNotes) : IRequest;
public class UpdateBookingStatusCommandHandler(IAppDbContext db, IBackgroundEmailDispatcher mailDispatch)
    : IRequestHandler<UpdateBookingStatusCommand>
{
    public async Task Handle(UpdateBookingStatusCommand request, CancellationToken cancellationToken)
    {
        var booking = await db.ServiceBookings.Include(b => b.FieldService)
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Booking not found.");

        var previous = booking.Status;
        booking.Status = request.Status;
        if (request.AdminNotes != null) booking.AdminNotes = request.AdminNotes;
        if (request.Status is ServiceBookingStatus.Confirmed or ServiceBookingStatus.Completed)
            booking.HoldExpiresAtUtc = null;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        if (previous == request.Status) return;

        var tz = SchedulingDefaults.BelgradeTz;
        var startLocal = TimeZoneInfo.ConvertTimeFromUtc(booking.StartUtc, tz).ToString("yyyy-MM-dd HH:mm");
        var endLocalFull = TimeZoneInfo.ConvertTimeFromUtc(booking.EndUtc, tz).ToString("yyyy-MM-dd HH:mm");
        var endLocalShort = TimeZoneInfo.ConvertTimeFromUtc(booking.EndUtc, tz).ToString("HH:mm");

        var serviceName = booking.FieldService.Name;
        var customerName = booking.CustomerName;
        var customerPhone = booking.CustomerPhone;
        var customerEmail = booking.CustomerEmail;
        var address = booking.Address;
        var newStatus = request.Status;

        mailDispatch.Enqueue(async (email, ct) =>
        {
            await BookingMail.NotifyAdminStatusChangeAsync(
                email, previous, newStatus, serviceName, customerName,
                customerPhone, customerEmail, address, startLocal, endLocalFull, ct);

            if (newStatus == ServiceBookingStatus.Confirmed)
            {
                await BookingMail.NotifyConfirmedAsync(
                    email, serviceName, customerName, customerPhone, customerEmail,
                    address, startLocal, endLocalShort, notifyAdmin: false, adminSource: null, ct);
            }
            else if (newStatus == ServiceBookingStatus.Cancelled)
            {
                await BookingMail.NotifyCancelledAsync(
                    email, serviceName, customerName, customerEmail, startLocal, endLocalShort, ct);
            }
        });
    }
}
