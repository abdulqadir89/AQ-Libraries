namespace AQ.Events.Outbox;

/// <summary>
/// Service for managing outbox events - storing and processing them.
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Gets unprocessed outbox events that are ready for processing.
    /// </summary>
    /// <param name="category">The category of events to retrieve (optional, null for all).</param>
    /// <param name="batchSize">Maximum number of events to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of unprocessed outbox events.</returns>
    Task<List<OutboxEvent>> GetUnprocessedEventsAsync(OutboxEventCategory? category = null, int batchSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a batch of outbox events by dispatching them to their handlers.
    /// </summary>
    /// <param name="events">The events to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ProcessEventsAsync(IEnumerable<OutboxEvent> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an event as successfully processed.
    /// </summary>
    /// <param name="eventId">The ID of the event to mark as processed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task MarkEventAsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a processing failure for an event.
    /// </summary>
    /// <param name="eventId">The ID of the event that failed.</param>
    /// <param name="error">The error message.</param>
    /// <param name="nextRetryDelay">The delay before the next retry attempt.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task RecordEventFailureAsync(Guid eventId, string error, TimeSpan nextRetryDelay, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old processed events from the outbox.
    /// </summary>
    /// <param name="olderThan">Remove events processed before this date.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CleanupProcessedEventsAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default);
}