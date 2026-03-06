using AQ.Abstractions;

namespace AQ.Events.Outbox;

/// <summary>
/// Categories of events that can be stored in the outbox.
/// </summary>
public enum OutboxEventCategory
{
    /// <summary>
    /// Domain events that handle internal business logic.
    /// </summary>
    Domain = 1,

    /// <summary>
    /// Integration events for cross-service communication.
    /// </summary>
    Integration = 2
}

/// <summary>
/// Represents an event stored in the outbox for reliable processing.
/// This entity implements the outbox pattern to ensure events are persisted
/// and processed reliably, even in case of system failures.
/// Supports both domain events and integration events.
/// </summary>
public class OutboxEvent : IEntity
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets the category of the event (Domain or Integration).
    /// </summary>
    public OutboxEventCategory Category { get; private set; }

    /// <summary>
    /// Gets the type of the event.
    /// </summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized domain event data.
    /// </summary>
    public string EventData { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the event occurred.
    /// </summary>
    public DateTimeOffset OccurredOn { get; private set; }

    /// <summary>
    /// Gets or sets when the event was processed.
    /// </summary>
    public DateTimeOffset? ProcessedOn { get; private set; }

    /// <summary>
    /// Gets or sets whether the event has been processed.
    /// </summary>
    public bool IsProcessed { get; private set; }

    /// <summary>
    /// Gets or sets the number of processing attempts.
    /// </summary>
    public int ProcessingAttempts { get; private set; }

    /// <summary>
    /// Gets or sets the last error that occurred during processing.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Gets or sets when the event should be processed next (for retry scenarios).
    /// </summary>
    public DateTimeOffset? NextProcessingAttempt { get; private set; }

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private OutboxEvent() { }

    /// <summary>
    /// Creates a new outbox event from any event with the required properties.
    /// </summary>
    /// <param name="category">The category of the event (Domain or Integration).</param>
    /// <param name="eventType">The type of the event.</param>
    /// <param name="eventData">The serialized event data.</param>
    /// <param name="occurredOn">When the event occurred.</param>
    public OutboxEvent(OutboxEventCategory category, string eventType, string eventData, DateTimeOffset occurredOn)
    {
        Category = category;
        EventType = eventType;
        EventData = eventData;
        OccurredOn = occurredOn;
        IsProcessed = false;
        ProcessingAttempts = 0;
    }

    /// <summary>
    /// Marks the event as successfully processed.
    /// </summary>
    public void MarkAsProcessed()
    {
        IsProcessed = true;
        ProcessedOn = DateTimeOffset.UtcNow;
        LastError = null;
        NextProcessingAttempt = null;
    }

    /// <summary>
    /// Records a processing failure and schedules a retry.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="nextRetryDelay">The delay before the next retry attempt.</param>
    public void RecordFailure(string error, TimeSpan nextRetryDelay)
    {
        ProcessingAttempts++;
        LastError = error;
        NextProcessingAttempt = DateTimeOffset.UtcNow.Add(nextRetryDelay);
    }

    /// <summary>
    /// Checks if the event is ready for processing (either first attempt or retry time has passed).
    /// </summary>
    public bool IsReadyForProcessing()
    {
        if (IsProcessed)
            return false;

        return NextProcessingAttempt == null || NextProcessingAttempt <= DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Checks if the event has exceeded the maximum retry attempts.
    /// </summary>
    /// <param name="maxRetryAttempts">Maximum number of retry attempts allowed.</param>
    public bool HasExceededMaxRetries(int maxRetryAttempts)
    {
        return ProcessingAttempts >= maxRetryAttempts;
    }
}