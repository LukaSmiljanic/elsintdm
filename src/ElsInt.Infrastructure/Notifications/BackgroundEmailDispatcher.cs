using ElsInt.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ElsInt.Infrastructure.Notifications;

public sealed class BackgroundEmailDispatcher : IBackgroundEmailDispatcher
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<BackgroundEmailDispatcher> _logger;

    public BackgroundEmailDispatcher(IServiceScopeFactory scopes, ILogger<BackgroundEmailDispatcher> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public void Enqueue(Func<IEmailNotifier, CancellationToken, Task> action)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var email = scope.ServiceProvider.GetRequiredService<IEmailNotifier>();
                await action(email, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background email dispatch failed");
            }
        });
    }
}
