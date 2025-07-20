using AQ.Common.Application.Services;
using AQ.Common.Domain.Entities;
using AQ.Common.Domain.Events;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AQ.Common.Infrastructure.Services;

/// <summary>
/// Outbox pattern implementation of domain event dispatcher.
/// This dispatcher stores events in the outbox for reliable, transactional processing.
/// Events are persisted as part of the business transaction and processed by a background service.
/// </summary>
public class OutboxDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<OutboxDomainEventDispatcher> _logger;

    public OutboxDomainEventDispatcher(IApplicationDbContext dbContext, ILogger<OutboxDomainEventDispatcher> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Stores all pending domain events from entities in the outbox for later processing.
    /// </summary>
    /// <param name="entities">The entities with pending domain events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DispatchEventsAsync(IEnumerable<IHasDomainEvents> entities, CancellationToken cancellationToken = default)
    {
        var domainEvents = entities
            .SelectMany(entity => entity.DomainEvents)
            .ToList();

        if (!domainEvents.Any())
        {
            return;
        }

        _logger.LogDebug("Storing {EventCount} domain events in outbox", domainEvents.Count);

        // Store each event in the outbox
        foreach (var domainEvent in domainEvents)
        {
            await StoreEventInOutboxAsync(domainEvent, cancellationToken);
        }

        // Clear events from entities after storing them to prevent re-processing
        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }

        _logger.LogDebug("Successfully stored {EventCount} domain events in outbox", domainEvents.Count);
    }

    /// <summary>
    /// Stores a single domain event in the outbox for later processing.
    /// </summary>
    /// <param name="domainEvent">The domain event to store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await StoreEventInOutboxAsync(domainEvent, cancellationToken);
    }

    private Task StoreEventInOutboxAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            // Serialize the domain event
            var eventData = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            // Create outbox event
            var outboxEvent = new OutboxEvent(domainEvent, eventData);
            
            // Store in database
            _dbContext.Add(outboxEvent);

            _logger.LogDebug("Stored domain event {EventType} with ID {EventId} in outbox", 
                domainEvent.GetType().Name, domainEvent.AggregateId);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store domain event {EventType} in outbox", domainEvent.GetType().Name);
            throw;
        }
    }
}
