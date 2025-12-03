using AQ.Abstractions;
using AQ.Events.Domain;
using System.ComponentModel.DataAnnotations.Schema;

namespace AQ.Entities;

public abstract class Entity : IEntity, IHasDomainEvents, IAuditable
{
    [NotMapped]
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; } = Guid.CreateVersion7();
    public Guid? CreatedById { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public Guid? UpdatedById { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
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
    public void RemoveEvents(IEnumerable<IDomainEvent> domainEvents)
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
            CreatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Mark the entity as updated by the given user and increment Revision.
    /// </summary>
    /// <param name="userId">Updater user id, or null for unauthenticated</param>
    public void SetUpdatedBy(Guid? userId)
    {
        UpdatedById = userId;
        UpdatedAt = DateTime.UtcNow;
        Revision++;
    }
}
