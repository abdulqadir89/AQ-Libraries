using AQ.Events.Dispatchers;
using AQ.Events.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AQ.EntityFramework.Extensions;

/// <summary>
/// Runs a handler again, from a clean change tracker, when its save loses a race on a concurrency token. Only the
/// handler that failed runs again; handlers that already ran for the event are not called a second time.
/// </summary>
// RULE EVT-9
public sealed class ConcurrencyRetryHandlerInvoker<TContext>(TContext context, ILogger<ConcurrencyRetryHandlerInvoker<TContext>> logger, int maxAttempts)
    : IDomainEventHandlerInvoker
    where TContext : DbContext
{
    public async Task InvokeAsync(IDomainEventHandler handler, IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await handler.HandleAsync(domainEvent, cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Handler {Handler} lost a race with another save while handling {EventType}; running it again (attempt {Attempt} of {MaxAttempts})",
                    handler.GetType().Name, domainEvent.GetType().Name, attempt + 1, maxAttempts);
                context.ChangeTracker.Clear();
            }
        }
    }
}

public static class DomainEventHandlerRetryExtensions
{
    /// <summary>
    /// Makes the dispatcher run each handler through <see cref="ConcurrencyRetryHandlerInvoker{TContext}"/>. The handler
    /// must load what it changes itself, because the change tracker of <typeparamref name="TContext"/> is cleared before every retry.
    /// </summary>
    public static IServiceCollection AddDomainEventHandlerConcurrencyRetry<TContext>(this IServiceCollection services, int maxAttempts = 3)
        where TContext : DbContext
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);
        services.Replace(ServiceDescriptor.Scoped<IDomainEventHandlerInvoker>(provider => new ConcurrencyRetryHandlerInvoker<TContext>(
            provider.GetRequiredService<TContext>(),
            provider.GetRequiredService<ILogger<ConcurrencyRetryHandlerInvoker<TContext>>>(),
            maxAttempts)));
        return services;
    }
}
