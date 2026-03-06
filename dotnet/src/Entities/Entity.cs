using AQ.Abstractions;
using AQ.Events.Domain;
using System.ComponentModel.DataAnnotations.Schema;

namespace AQ.Entities;

public abstract class Entity : IEntity, IHasDomainEvents, IAuditable, IResourceCreator
{
    [NotMapped]
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; } = Guid.CreateVersion7();
    public Guid? CreatedById { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedById { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public int Revision { get; private set; }
    /// <summary>
    /// Gets the collection of domain events raised by this entity.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to this entity.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Adds multiple domain events to this entity.
    /// </summary>
    /// <param name="domainEvents">The domain events to add.</param>
    public void AddDomainEvents(IEnumerable<IDomainEvent> domainEvents)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);
        _domainEvents.AddRange(domainEvents);
    }

    /// <summary>
    /// Removes a domain event from this entity.
    /// </summary>
    /// <param name="domainEvent">The domain event to remove.</param>
    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Removes multiple domain events from this entity.
    /// </summary>
    /// <param name="domainEvents">The domain events to remove.</param>
    public void RemoveDomainEvents(IEnumerable<IDomainEvent> domainEvents)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);
        foreach (var domainEvent in domainEvents)
        {
            _domainEvents.Remove(domainEvent);
        }
    }

    /// <summary>
    /// Clears all domain events from this entity.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Mark the entity as created by given user. Does not overwrite existing CreatedById.
    /// </summary>
    /// <param name="userId">Creator user id, or null for unauthenticated</param>
    public void SetCreatedBy(Guid? userId)
    {
        if (CreatedById is null)
        {
            CreatedById = userId;
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Mark the entity as updated by the given user and increment Revision.
    /// </summary>
    /// <param name="userId">Updater user id, or null for unauthenticated</param>
    public void SetUpdatedBy(Guid? userId)
    {
        UpdatedById = userId;
        UpdatedAt = DateTimeOffset.UtcNow;
        Revision++;
    }

    /// <summary>
    /// Lifecycle method called when the entity is added to the repository. Can be used to perform actions like raising domain events.
    /// Note: This is called every time the entity is added to the repository, even if it's already persisted. Use with caution.
    /// </summary>
    public virtual void OnAdd()
    {
    }

    /// <summary> Lifecycle method called when the entity is updated. Can be used to perform actions like raising domain events.
    /// Note: This is called every time the entity is updated in the repository, even if it's already persisted. Use with caution.
    /// </summary>
    public virtual void OnUpdate()
    {
    }

    /// <summary>
    /// Lifecycle method called when the entity is removed from the repository. Can be used to perform actions like raising domain events or checking constraints before deletion.
    /// Note: This is called every time the entity is removed from the repository, even if it's already persisted. Use with caution.
    /// </summary>
    public virtual void OnRemove()
    {
        if (!CanRemove())
        {
            throw new InvalidOperationException($"Entity of type {GetType().Name} with ID {Id} cannot be removed due to business constraints.");
        }
    }

    /// <summary>
    /// Lifecycle method called to check if the entity can be removed from the repository. Can be used to perform actions like checking constraints before deletion.
    /// Note: This is called every time the entity is checked for removal, even if it's already persisted. Use with caution.
    /// </summary>
    public virtual bool CanRemove()
    {
        return true;
    }
}
