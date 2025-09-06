namespace AQ.Events.Domain;

/// <summary>
/// Marker interface for domain events.
/// All domain events should implement this interface.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// The timestamp when this event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// The unique identifier of the aggregate that raised this event.
    /// </summary>
    Guid AggregateId { get; }
}
