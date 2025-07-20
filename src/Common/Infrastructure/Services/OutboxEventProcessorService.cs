using AQ.Common.Application.Services;
using AQ.Common.Domain.Entities;
using AQ.Common.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AQ.Common.Infrastructure.Services;

/// <summary>
/// Background service that processes outbox events.
/// This service runs periodically to process stored domain events from the outbox,
/// ensuring reliable delivery even in case of temporary failures.
/// </summary>
public class OutboxEventProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxEventProcessorService> _logger;
    private readonly TimeSpan _processingInterval;
    private readonly int _maxRetryAttempts;
    private readonly TimeSpan _baseRetryDelay;

    public OutboxEventProcessorService(
        IServiceProvider serviceProvider, 
        ILogger<OutboxEventProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _processingInterval = TimeSpan.FromSeconds(30); // Process every 30 seconds
        _maxRetryAttempts = 5; // Maximum retry attempts
        _baseRetryDelay = TimeSpan.FromMinutes(1); // Base delay for retries (exponential backoff)
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Event Processor Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing outbox events");
            }

            // Wait before next processing cycle
            await Task.Delay(_processingInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        
        // Get unprocessed events that are ready for processing
        var outboxEvents = await GetReadyOutboxEventsAsync(dbContext, cancellationToken);

        if (!outboxEvents.Any())
        {
            _logger.LogDebug("No outbox events ready for processing");
            return;
        }

        _logger.LogDebug("Processing {EventCount} outbox events", outboxEvents.Count);

        foreach (var outboxEvent in outboxEvents)
        {
            await ProcessSingleEventAsync(scope.ServiceProvider, outboxEvent, cancellationToken);
        }

        // Save changes to update event processing status
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<OutboxEvent>> GetReadyOutboxEventsAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        // This is a simplified query - in a real implementation, you'd want to use a proper specification
        // or repository pattern to fetch unprocessed outbox events
        var allUnprocessedEvents = await dbContext.Set<OutboxEvent>()
            .Where(e => !e.IsProcessed)
            .OrderBy(e => e.OccurredOn)
            .Take(100) // Process in batches
            .ToListAsync(cancellationToken);

        var readyEvents = allUnprocessedEvents
            .Where(e => e.IsReadyForProcessing() && !e.HasExceededMaxRetries(_maxRetryAttempts))
            .ToList();

        return readyEvents;
    }

    private async Task ProcessSingleEventAsync(IServiceProvider serviceProvider, OutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing outbox event {EventId} of type {EventType}", 
                outboxEvent.Id, outboxEvent.EventType);

            // Deserialize the domain event
            var domainEvent = DeserializeDomainEvent(outboxEvent);
            
            if (domainEvent == null)
            {
                _logger.LogWarning("Failed to deserialize outbox event {EventId} of type {EventType}",
                    outboxEvent.Id, outboxEvent.EventType);
                
                outboxEvent.RecordFailure("Failed to deserialize event", CalculateRetryDelay(outboxEvent.ProcessingAttempts));
                return;
            }

            // Find and execute handlers
            await ExecuteEventHandlersAsync(serviceProvider, domainEvent, cancellationToken);

            // Mark as processed
            outboxEvent.MarkAsProcessed();
            
            _logger.LogDebug("Successfully processed outbox event {EventId} of type {EventType}",
                outboxEvent.Id, outboxEvent.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process outbox event {EventId} of type {EventType}",
                outboxEvent.Id, outboxEvent.EventType);

            var retryDelay = CalculateRetryDelay(outboxEvent.ProcessingAttempts);
            outboxEvent.RecordFailure(ex.Message, retryDelay);

            if (outboxEvent.HasExceededMaxRetries(_maxRetryAttempts))
            {
                _logger.LogError("Outbox event {EventId} has exceeded maximum retry attempts ({MaxRetries})",
                    outboxEvent.Id, _maxRetryAttempts);
            }
        }
    }

    private IDomainEvent? DeserializeDomainEvent(OutboxEvent outboxEvent)
    {
        try
        {
            // Get the event type
            var eventType = Type.GetType(outboxEvent.EventType);
            if (eventType == null)
            {
                _logger.LogWarning("Could not resolve event type: {EventType}", outboxEvent.EventType);
                return null;
            }

            // Deserialize the event data
            var domainEvent = JsonSerializer.Deserialize(outboxEvent.EventData, eventType, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) as IDomainEvent;

            return domainEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing outbox event {EventId}", outboxEvent.Id);
            return null;
        }
    }

    private async Task ExecuteEventHandlersAsync(IServiceProvider serviceProvider, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        
        // Find all handlers for this domain event type
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handlers = serviceProvider.GetServices(handlerType);

        if (!handlers.Any())
        {
            _logger.LogWarning("No handlers found for domain event type: {EventType}", eventType.Name);
            return;
        }

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

        _logger.LogDebug("Successfully executed {HandlerCount} handlers for event type: {EventType}",
            handlersList.Count, eventType.Name);
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handler {HandlerType} failed to process event {EventType}",
                handler.GetType().Name, domainEvent.GetType().Name);
            throw;
        }
    }

    private TimeSpan CalculateRetryDelay(int attempt)
    {
        // Exponential backoff with jitter
        var delay = TimeSpan.FromMilliseconds(_baseRetryDelay.TotalMilliseconds * Math.Pow(2, attempt));
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, (int)(delay.TotalMilliseconds * 0.1)));
        return delay.Add(jitter);
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Event Processor Service is stopping");
        await base.StopAsync(stoppingToken);
    }
}
