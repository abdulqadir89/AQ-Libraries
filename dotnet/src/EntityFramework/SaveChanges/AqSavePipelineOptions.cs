using System.Text.Json;

namespace AQ.EntityFramework.SaveChanges;

public sealed class AqSavePipelineOptions
{
    public bool Lifecycle { get; init; }
    public bool VersionGroups { get; init; }
    public bool Tags { get; init; }
    public bool GeneratedReferences { get; init; }
    public bool VerificationReset { get; init; }
    public bool Review { get; init; }
    public bool Audit { get; init; }
    public bool Snapshots { get; init; }
    public bool Outbox { get; init; }

    /// <summary>Append an <c>AuditEvent</c> for every change to an <c>[Audited]</c> entity. Needs an active actor.</summary>
    public bool AuditLog { get; init; }

    /// <summary>Serializer options for outbox event payloads. Defaults to <see cref="JsonSerializerOptions.Default"/> behaviour.</summary>
    public JsonSerializerOptions? OutboxJson { get; init; }
}
