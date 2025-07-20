using AQ.Common.Domain.Entities;
using AQ.Common.Domain.Events;

namespace AQ.Common.Domain.Extensions;

/// <summary>
/// Extension methods for Entity to simplify domain event management.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Raises an EntityCreatedEvent for this entity.
    /// </summary>
    /// <param name="entity">The entity that was created.</param>
    /// <param name="createdBy">The ID of the user who created the entity.</param>
    public static void RaiseCreatedEvent(this Entity entity, Guid? createdBy = null)
    {
        var entityType = entity.GetType().Name;
        entity.AddDomainEvent(new EntityCreatedEvent(entity.Id, entityType, createdBy));
    }

    /// <summary>
    /// Raises an EntityUpdatedEvent for this entity.
    /// </summary>
    /// <param name="entity">The entity that was updated.</param>
    /// <param name="updatedBy">The ID of the user who updated the entity.</param>
    /// <param name="changedProperties">The properties that were changed.</param>
    public static void RaiseUpdatedEvent(this Entity entity, Guid? updatedBy = null, IReadOnlyList<string>? changedProperties = null)
    {
        var entityType = entity.GetType().Name;
        entity.AddDomainEvent(new EntityUpdatedEvent(entity.Id, entityType, updatedBy, changedProperties));
    }

    /// <summary>
    /// Raises an EntityDeletedEvent for this entity.
    /// </summary>
    /// <param name="entity">The entity that was deleted.</param>
    /// <param name="deletedBy">The ID of the user who deleted the entity.</param>
    public static void RaiseDeletedEvent(this Entity entity, Guid? deletedBy = null)
    {
        var entityType = entity.GetType().Name;
        entity.AddDomainEvent(new EntityDeletedEvent(entity.Id, entityType, deletedBy));
    }
}
