using AQ.Events.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AQ.Events.Dispatchers;

/// <summary>
/// Implementation of domain event dispatcher that processes events immediately.
/// This dispatcher finds all registered handlers for each domain event and executes them.
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

    /// <summary>
    /// Dispatches all provided domain events.
    /// </summary>
    /// <param name="domainEvents">The domain events to dispatch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        // Process each domain event
        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }

    /// <summary>
    /// Dispatches a single domain event to all registered handlers.
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType();
        _logger.LogDebug("Dispatching domain event: {EventType}", eventType.Name);

        try
        {
            // Find all handler concrete types registered for this event
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlerConcreteTypes = _serviceProvider.GetServices(handlerType)
                .Where(h => h != null)
                .Select(h => h!.GetType())
                .ToList();

            // Execute each handler sequentially in its own scope for an isolated DbContext
            foreach (var concreteType in handlerConcreteTypes)
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedHandler = scope.ServiceProvider.GetServices(handlerType)
                    .FirstOrDefault(h => h?.GetType() == concreteType);
                if (scopedHandler is null) continue;
                await InvokeHandlerAsync(scopedHandler, domainEvent, cancellationToken);
            }

            _logger.LogDebug("Successfully dispatched domain event: {EventType} to {HandlerCount} handlers",
                eventType.Name, handlerConcreteTypes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching domain event: {EventType}. Event data: {EventData}",
                eventType.Name, JsonSerializer.Serialize(domainEvent));
            throw;
        }
    }

    private async Task InvokeHandlerAsync(object handler, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            // Use reflection to call HandleAsync method
            var method = handler.GetType().GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));
            if (method != null)
            {
                var task = method.Invoke(handler, new object[] { domainEvent, cancellationToken });
                if (task is Task asyncTask)
                {
                    await asyncTask;
                }
            }

            _logger.LogDebug("Handler {HandlerType} successfully processed event {EventType}",
                handler.GetType().Name, domainEvent.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handler {HandlerType} failed to process event {EventType}",
                handler.GetType().Name, domainEvent.GetType().Name);
            throw;
        }
    }
}
