namespace ElsInt.Application.Interfaces;

public interface IEmailNotifier
{
    /// <summary>Notify configured admin inbox(es) — Email:NotifyTo, comma-separated.</summary>
    Task NotifyAsync(string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>Send to an arbitrary address. No-op when email is disabled or address empty.</summary>
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
