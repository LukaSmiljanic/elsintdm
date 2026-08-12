using ElsInt.Application.Interfaces;
using ElsInt.Domain.Enums;

namespace ElsInt.Application.Scheduling;

internal static class BookingMail
{
    public static async Task NotifyNewPublicHoldAsync(
        IEmailNotifier email,
        string serviceName,
        string customerName,
        string customerPhone,
        string? customerEmail,
        string? address,
        string? notes,
        string startLocal,
        string endLocal,
        DateTime? holdExpiresAtUtc,
        string adminBookingsUrl,
        CancellationToken cancellationToken)
    {
        await email.NotifyAsync(
            $"Novi termin (hold): {serviceName} {startLocal}",
            $"""
            Nova online rezervacija — privremeni hold.

            Usluga: {serviceName}
            Termin: {startLocal} – {endLocal}
            Klijent: {customerName}
            Telefon: {customerPhone}
            Email: {customerEmail ?? "—"}
            Adresa: {address ?? "—"}
            Napomena: {notes ?? "—"}
            Hold ističe: {holdExpiresAtUtc:dd.MM.yyyy HH:mm} UTC

            Potvrdite u adminu: {adminBookingsUrl}
            """,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(customerEmail)) return;

        await email.SendAsync(
            customerEmail,
            $"ElsInt — zahtev za termin primljen ({serviceName})",
            $"""
            Poštovani/a {customerName},

            primili smo zahtev za termin:

            Usluga: {serviceName}
            Termin: {startLocal} – {endLocal}
            Adresa: {address ?? "—"}

            Termin je privremeno rezervisan. Potvrdićemo ga telefonom u roku od nekoliko sati.
            Ako ne stignemo da potvrdimo, rezervacija ističe automatski.

            Za hitne izmene pozovite nas na +381 67 762 7904.

            Srdačan pozdrav,
            ElsInt
            """,
            cancellationToken);
    }

    public static async Task NotifyConfirmedAsync(
        IEmailNotifier email,
        string serviceName,
        string customerName,
        string customerPhone,
        string? customerEmail,
        string? address,
        string startLocal,
        string endLocal,
        bool notifyAdmin,
        string? adminSource,
        CancellationToken cancellationToken)
    {
        if (notifyAdmin)
        {
            await email.NotifyAsync(
                $"Termin potvrđen: {serviceName} {startLocal}",
                $"""
                {(adminSource ?? "Termin potvrđen")}.

                Usluga: {serviceName}
                Termin: {startLocal} – {endLocal}
                Klijent: {customerName}
                Telefon: {customerPhone}
                Email: {customerEmail ?? "—"}
                Adresa: {address ?? "—"}
                """,
                cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(customerEmail)) return;

        await email.SendAsync(
            customerEmail,
            $"ElsInt — termin potvrđen ({serviceName})",
            $"""
            Poštovani/a {customerName},

            potvrđujemo Vaš termin:

            Usluga: {serviceName}
            Termin: {startLocal} – {endLocal}
            Adresa: {address ?? "—"}

            Vidimo se! Za izmene ili otkazivanje: +381 67 762 7904.

            Srdačan pozdrav,
            ElsInt
            """,
            cancellationToken);
    }

    public static async Task NotifyCancelledAsync(
        IEmailNotifier email,
        string serviceName,
        string customerName,
        string? customerEmail,
        string startLocal,
        string endLocal,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerEmail)) return;

        await email.SendAsync(
            customerEmail,
            $"ElsInt — termin otkazan ({serviceName})",
            $"""
            Poštovani/a {customerName},

            Vaš termin je otkazan:

            Usluga: {serviceName}
            Termin: {startLocal} – {endLocal}

            Za novo zakazivanje: https://elsintdm.rs/zakazivanje
            ili pozovite +381 67 762 7904.

            Srdačan pozdrav,
            ElsInt
            """,
            cancellationToken);
    }

    /// <summary>Admin inbox when status changes in Termini (customer gets their own mail when applicable).</summary>
    public static async Task NotifyAdminStatusChangeAsync(
        IEmailNotifier email,
        ServiceBookingStatus previous,
        ServiceBookingStatus current,
        string serviceName,
        string customerName,
        string customerPhone,
        string? customerEmail,
        string? address,
        string startLocal,
        string endLocal,
        CancellationToken cancellationToken)
    {
        await email.NotifyAsync(
            $"Termin: {StatusLabel(previous)} → {StatusLabel(current)} ({serviceName})",
            $"""
            Promena statusa termina u adminu.

            Status: {StatusLabel(previous)} → {StatusLabel(current)}
            Usluga: {serviceName}
            Termin: {startLocal} – {endLocal}
            Klijent: {customerName}
            Telefon: {customerPhone}
            Email klijenta: {customerEmail ?? "—"}
            Adresa: {address ?? "—"}
            """,
            cancellationToken);
    }

    public static string StatusLabel(ServiceBookingStatus status) => status switch
    {
        ServiceBookingStatus.Held => "Rezervisan (hold)",
        ServiceBookingStatus.Confirmed => "Potvrđen",
        ServiceBookingStatus.Completed => "Završen",
        ServiceBookingStatus.Cancelled => "Otkazan",
        ServiceBookingStatus.Expired => "Istekao",
        _ => status.ToString()
    };
}
