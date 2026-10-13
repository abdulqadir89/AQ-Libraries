using Microsoft.Extensions.DependencyInjection;

namespace AQ.Actors;

/// <summary>Creates a DI scope and an actor scope together, so scoped services (DbContext) always run under an actor.</summary>
public interface IActorScopeFactory
{
    Task RunAsync(Actor actor, Func<IServiceProvider, CancellationToken, Task> work, CancellationToken ct = default);

    Task<T> RunAsync<T>(Actor actor, Func<IServiceProvider, CancellationToken, Task<T>> work, CancellationToken ct = default);
}

public sealed class ActorScopeFactory(IServiceScopeFactory scopes, IActorContext actors) : IActorScopeFactory
{
    public async Task RunAsync(Actor actor, Func<IServiceProvider, CancellationToken, Task> work, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        await using var scope = scopes.CreateAsyncScope();
        using var _ = actors.Begin(actor);
        await work(scope.ServiceProvider, ct);
    }

    public async Task<T> RunAsync<T>(Actor actor, Func<IServiceProvider, CancellationToken, Task<T>> work, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        await using var scope = scopes.CreateAsyncScope();
        using var _ = actors.Begin(actor);
        return await work(scope.ServiceProvider, ct);
    }
}
