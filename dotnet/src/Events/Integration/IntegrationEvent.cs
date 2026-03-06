namespace AQ.Events.Integration;

/// <summary>
/// Base class for integration events.
/// Provides common properties and behavior for all integration events.
/// </summary>
public abstract class IntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationEvent"/> class.
    /// </summary>
    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationEvent"/> class with the specified ID and occurrence time.
    /// </summary>
    /// <param name="id">The unique identifier for the event.</param>
    /// <param name="occurredOn">The date and time when the event occurred.</param>
    protected IntegrationEvent(Guid id, DateTimeOffset occurredOn)
    {
        Id = id;
        OccurredOn = occurredOn;
    }

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public DateTimeOffset OccurredOn { get; }
}
