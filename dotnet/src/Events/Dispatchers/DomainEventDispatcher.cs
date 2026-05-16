using AQ.Events.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AQ.Events.Dispatchers;

/// <summary>
/// Dispatches domain events to all registered handlers sequentially.
///
/// Handler isolation:
///   Each handler is responsible for its own data access lifetime.
///   Inject <c>IDbContextFactory&lt;TContext&gt;</c> in handlers — not the context directly —
///   and create the context inside <c>HandleAsync</c>. This keeps handlers fully isolated
///   from each other regardless of how many handle the same event.
///
/// Scoping:
///   Handlers are resolved from the same DI scope as the dispatcher. Because handlers
///   manage their own DbContext via the factory, no per-handler child scope is needed.
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
            await DispatchAsync(domainEvent, cancellationToken);
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType();
        _logger.LogDebug("Dispatching domain event: {EventType}", eventType.Name);

        try
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = _serviceProvider.GetServices(handlerType)
                .OfType<IDomainEventHandler>()
                .ToList();

            foreach (var handler in handlers)
                await handler.HandleAsync(domainEvent, cancellationToken);

            _logger.LogDebug("Dispatched {EventType} to {Count} handler(s)", eventType.Name, handlers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching domain event: {EventType}. Event data: {EventData}",
                eventType.Name, JsonSerializer.Serialize(domainEvent));
            throw;
        }
    }
}
