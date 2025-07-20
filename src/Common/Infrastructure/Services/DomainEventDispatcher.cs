using AQ.Common.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AQ.Common.Infrastructure.Services;

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
    /// Dispatches all pending domain events from the given entities.
    /// </summary>
    /// <param name="entities">The entities with pending domain events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DispatchEventsAsync(IEnumerable<IHasDomainEvents> entities, CancellationToken cancellationToken = default)
    {
        var domainEvents = entities
            .SelectMany(entity => entity.DomainEvents)
            .ToList();

        // Clear events from entities after collecting them to prevent re-processing
        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }

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
            // Find all handlers for this domain event type
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = _serviceProvider.GetServices(handlerType);

            // Execute all handlers
            var handlersList = handlers.ToList();
            var tasks = new List<Task>();
            
            foreach (var handler in handlersList)
            {
                if (handler != null)
                {
                    tasks.Add(InvokeHandlerAsync(handler, domainEvent, cancellationToken));
                }
            }
            
            await Task.WhenAll(tasks);

            _logger.LogDebug("Successfully dispatched domain event: {EventType} to {HandlerCount} handlers", 
                eventType.Name, handlersList.Count);
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
