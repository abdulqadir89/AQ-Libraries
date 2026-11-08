using AQ.Events.Dispatchers;
using AQ.Events.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AQ.EntityFramework.Extensions;

/// <summary>
/// Runs a handler again, from a clean change tracker, when its save loses a race on a concurrency token. Only the
/// handler that failed runs again; handlers that already ran for the event are not called a second time. With
/// <c>useTransaction</c> the handler runs in one database transaction (joining one already open), so everything it saved
/// is rolled back when it fails and a run again starts from the state before it.
/// </summary>
// RULE EVT-9
public sealed class ConcurrencyRetryHandlerInvoker<TContext>(
    TContext context,
    ILogger<ConcurrencyRetryHandlerInvoker<TContext>> logger,
    int maxAttempts,
    bool useTransaction = false)
    : IDomainEventHandlerInvoker
    where TContext : DbContext
{
    public async Task InvokeAsync(IDomainEventHandler handler, IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await RunAsync(handler, domainEvent, cancellationToken);
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

    private async Task RunAsync(IDomainEventHandler handler, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // RULE EVT-10
        await using var transaction = useTransaction && context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await handler.HandleAsync(domainEvent, cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }
}

public static class DomainEventHandlerRetryExtensions
{
    /// <summary>
    /// Makes the dispatcher run each handler through <see cref="ConcurrencyRetryHandlerInvoker{TContext}"/>. The handler
    /// must load what it changes itself, because the change tracker of <typeparamref name="TContext"/> is cleared before every retry.
    /// With <paramref name="useTransaction"/> each handler also runs in one transaction; the context must not use an execution strategy that retries.
    /// </summary>
    public static IServiceCollection AddDomainEventHandlerConcurrencyRetry<TContext>(this IServiceCollection services, int maxAttempts = 3, bool useTransaction = false)
        where TContext : DbContext
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);
        services.Replace(ServiceDescriptor.Scoped<IDomainEventHandlerInvoker>(provider => new ConcurrencyRetryHandlerInvoker<TContext>(
            provider.GetRequiredService<TContext>(),
            provider.GetRequiredService<ILogger<ConcurrencyRetryHandlerInvoker<TContext>>>(),
            maxAttempts,
            useTransaction)));
        return services;
    }
}
