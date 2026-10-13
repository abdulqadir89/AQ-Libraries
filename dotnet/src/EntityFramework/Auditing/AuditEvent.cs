using AQ.Actors;

namespace AQ.EntityFramework.Auditing;

public enum AuditOperation
{
    Insert = 1,
    Update = 2,
    Delete = 3,
}

/// <summary>
/// One row of the append-only audit log: what changed on an <c>[Audited]</c> entity and who did it. Written by the
/// save pipeline in the same transaction as the change. There is deliberately no way to edit or delete one through
/// the pipeline (it throws); retention is a separate concern.
/// </summary>
public class AuditEvent
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public AuditOperation Operation { get; private set; }

    /// <summary>JSON object keyed by property: <c>{ "Name": { "old": "a", "new": "b" } }</c>. Inserts have no old, deletes no new.</summary>
    public string ChangesJson { get; private set; } = "{}";

    public Guid ActorId { get; private set; }

    public PrincipalKind ActorKind { get; private set; }

    public Guid? OnBehalfOfId { get; private set; }

    public string? ClientId { get; private set; }

    public string? CorrelationId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    private AuditEvent() { }

    public AuditEvent(string entityType, Guid entityId, AuditOperation operation, string changesJson, Actor actor, DateTimeOffset occurredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentNullException.ThrowIfNull(actor);

        EntityType = entityType;
        EntityId = entityId;
        Operation = operation;
        ChangesJson = changesJson;
        ActorId = actor.PrincipalId;
        ActorKind = actor.Kind;
        OnBehalfOfId = actor.OnBehalfOfId;
        ClientId = actor.ClientId;
        CorrelationId = actor.CorrelationId;
        OccurredAt = occurredAt;
    }
}
