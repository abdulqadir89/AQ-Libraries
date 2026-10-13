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
    /// The principal that caused this event. Set by the save pipeline, never by callers.
    /// </summary>
    Guid? ActorId { get; set; }

    /// <summary>
    /// The principal the actor was acting for, when delegated. Set by the save pipeline.
    /// </summary>
    Guid? OnBehalfOfId { get; set; }

    /// <summary>
    /// Trace id tying this event to the request or job that raised it. Set by the save pipeline.
    /// </summary>
    string? CorrelationId { get; set; }
}
