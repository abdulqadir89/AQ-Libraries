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

    /// <summary>
    /// The identifier of the user who caused this event, if applicable.
    /// This can be set by the application when raising the event.
    /// </summary>
    Guid? UserId { get; set; }
}
