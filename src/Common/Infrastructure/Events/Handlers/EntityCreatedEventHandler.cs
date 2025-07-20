using AQ.Common.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AQ.Common.Infrastructure.Events.Handlers;

/// <summary>
/// Example domain event handler that demonstrates how to handle domain events.
/// This handler processes EntityCreatedEvent to show the pattern.
/// </summary>
public class EntityCreatedEventHandler : IDomainEventHandler<EntityCreatedEvent>
{
    private readonly ILogger<EntityCreatedEventHandler> _logger;

    public EntityCreatedEventHandler(ILogger<EntityCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles the EntityCreatedEvent by logging the creation.
    /// In a real application, this might:
    /// - Send notifications
    /// - Update read models
    /// - Trigger external integrations
    /// - Update caches
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task HandleAsync(EntityCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Entity created: {EntityType} with ID {AggregateId} by user {CreatedBy}",
            domainEvent.EntityType,
            domainEvent.AggregateId,
            domainEvent.CreatedBy);

        // Example async operation - could be sending an email, updating a cache, etc.
        await Task.Delay(100, cancellationToken);

        _logger.LogDebug("Finished processing EntityCreatedEvent for {EntityType} {AggregateId}",
            domainEvent.EntityType,
            domainEvent.AggregateId);
    }
}
