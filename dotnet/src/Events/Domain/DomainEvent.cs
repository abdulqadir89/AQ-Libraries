namespace AQ.Events.Domain;

/// <summary>
/// Base record for domain events with common properties.
/// </summary>
public abstract record DomainEvent(Guid AggregateId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
