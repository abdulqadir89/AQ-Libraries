namespace AQ.Events.Outbox;

/// <summary>
/// Marker interface for events that can be stored in the outbox pattern.
/// Both domain events and integration events can implement this interface.
/// </summary>
public interface IOutboxEvent
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets when the event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}