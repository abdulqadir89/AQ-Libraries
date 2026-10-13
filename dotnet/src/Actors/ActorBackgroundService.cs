using Microsoft.Extensions.Hosting;

namespace AQ.Actors;

/// <summary>
/// Base for hosted services that touch the database. The only sanctioned way to reach a DbContext from
/// a hosted service is <see cref="RunScopedAsync(Func{IServiceProvider, CancellationToken, Task}, CancellationToken)"/>.
/// </summary>
public abstract class ActorBackgroundService(IActorScopeFactory scopeFactory) : BackgroundService
{
    /// <summary>The principal this service writes as.</summary>
    protected abstract Actor Actor { get; }

    protected Task RunScopedAsync(Func<IServiceProvider, CancellationToken, Task> work, CancellationToken ct)
        => scopeFactory.RunAsync(Actor, work, ct);

    protected Task<T> RunScopedAsync<T>(Func<IServiceProvider, CancellationToken, Task<T>> work, CancellationToken ct)
        => scopeFactory.RunAsync(Actor, work, ct);
}
