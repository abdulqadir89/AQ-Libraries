namespace AQ.Events.Integration;

/// <summary>
/// Marker interface for integration events.
/// Integration events are used for communication between bounded contexts.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// The date and time when the event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}
