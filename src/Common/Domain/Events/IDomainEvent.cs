namespace AQ.Common.Domain.Events;

/// <summary>
/// Marker interface for domain events.
/// All domain events should implement this interface.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// The timestamp when this event occurred.
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// The unique identifier of the aggregate that raised this event.
    /// </summary>
    Guid AggregateId { get; }
}

/// <summary>
/// Base record for domain events with common properties.
/// </summary>
public abstract record DomainEvent(Guid AggregateId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
