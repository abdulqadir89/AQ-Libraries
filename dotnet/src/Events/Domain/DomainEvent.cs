namespace AQ.Events.Domain;

/// <summary>
/// Base record for domain events with common properties.
/// </summary>
public abstract record DomainEvent(Guid AggregateId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public Guid? ActorId { get; set; }
    public Guid? OnBehalfOfId { get; set; }
    public string? CorrelationId { get; set; }
}
