namespace AQ.Common.Domain.Events;

/// <summary>
/// Domain event raised when an entity is created.
/// </summary>
/// <param name="AggregateId">The ID of the created entity.</param>
/// <param name="EntityType">The type name of the created entity.</param>
/// <param name="CreatedBy">The ID of the user who created the entity.</param>
public record EntityCreatedEvent(
    Guid AggregateId,
    string EntityType,
    Guid? CreatedBy = null
) : DomainEvent(AggregateId);

/// <summary>
/// Domain event raised when an entity is updated.
/// </summary>
/// <param name="AggregateId">The ID of the updated entity.</param>
/// <param name="EntityType">The type name of the updated entity.</param>
/// <param name="UpdatedBy">The ID of the user who updated the entity.</param>
/// <param name="ChangedProperties">The properties that were changed.</param>
public record EntityUpdatedEvent(
    Guid AggregateId,
    string EntityType,
    Guid? UpdatedBy = null,
    IReadOnlyList<string>? ChangedProperties = null
) : DomainEvent(AggregateId);

/// <summary>
/// Domain event raised when an entity is deleted.
/// </summary>
/// <param name="AggregateId">The ID of the deleted entity.</param>
/// <param name="EntityType">The type name of the deleted entity.</param>
/// <param name="DeletedBy">The ID of the user who deleted the entity.</param>
public record EntityDeletedEvent(
    Guid AggregateId,
    string EntityType,
    Guid? DeletedBy = null
) : DomainEvent(AggregateId);
