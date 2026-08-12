using ElsInt.Application.Interfaces;

namespace ElsInt.Application.Tests;

internal sealed class FakeEmailNotifier : IEmailNotifier
{
    public List<(string? To, string Subject, string Body)> Sent { get; } = [];

    public Task NotifyAsync(string subject, string body, CancellationToken cancellationToken = default)
    {
        Sent.Add(("__notify__", subject, body));
        return Task.CompletedTask;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        Sent.Add((to, subject, body));
        return Task.CompletedTask;
    }
}

// Runs enqueued mail inline so tests can assert Sent immediately.
internal sealed class SyncEmailDispatcher(IEmailNotifier email) : IBackgroundEmailDispatcher
{
    public void Enqueue(Func<IEmailNotifier, CancellationToken, Task> action)
        => action(email, default).GetAwaiter().GetResult();
}
