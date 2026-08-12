namespace ElsInt.Application.Interfaces;

/// <summary>Runs outbound email off the HTTP request thread so SMTP latency does not block admin actions.</summary>
public interface IBackgroundEmailDispatcher
{
    void Enqueue(Func<IEmailNotifier, CancellationToken, Task> action);
}
