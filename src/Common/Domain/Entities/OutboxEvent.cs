using AQ.Common.Domain.Events;

namespace AQ.Common.Domain.Entities;

/// <summary>
/// Represents a domain event stored in the outbox for reliable processing.
/// This entity implements the outbox pattern to ensure domain events are persisted
/// and processed reliably, even in case of system failures.
/// </summary>
public class OutboxEvent : Entity
{
    /// <summary>
    /// Gets or sets the type of the domain event.
    /// </summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized domain event data.
    /// </summary>
    public string EventData { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the event occurred.
    /// </summary>
    public DateTime OccurredOn { get; private set; }

    /// <summary>
    /// Gets or sets when the event was processed.
    /// </summary>
    public DateTime? ProcessedOn { get; private set; }

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
    public DateTime? NextProcessingAttempt { get; private set; }

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private OutboxEvent() { }

    /// <summary>
    /// Creates a new outbox event from a domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to store in the outbox.</param>
    /// <param name="eventData">The serialized event data.</param>
    public OutboxEvent(IDomainEvent domainEvent, string eventData)
    {
        EventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName ?? domainEvent.GetType().Name;
        EventData = eventData;
        OccurredOn = domainEvent.OccurredOn;
        IsProcessed = false;
        ProcessingAttempts = 0;
    }

    /// <summary>
    /// Marks the event as successfully processed.
    /// </summary>
    public void MarkAsProcessed()
    {
        IsProcessed = true;
        ProcessedOn = DateTime.UtcNow;
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
        NextProcessingAttempt = DateTime.UtcNow.Add(nextRetryDelay);
    }

    /// <summary>
    /// Checks if the event is ready for processing (either first attempt or retry time has passed).
    /// </summary>
    public bool IsReadyForProcessing()
    {
        if (IsProcessed)
            return false;

        return NextProcessingAttempt == null || NextProcessingAttempt <= DateTime.UtcNow;
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
