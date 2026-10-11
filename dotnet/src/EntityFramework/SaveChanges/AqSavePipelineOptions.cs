namespace AQ.EntityFramework.SaveChanges;

public sealed class AqSavePipelineOptions
{
    public bool Lifecycle { get; init; }
    public bool GeneratedReferences { get; init; }
    public bool VerificationReset { get; init; }
    public bool Audit { get; init; }
    public bool Snapshots { get; init; }
    public bool Outbox { get; init; }
}
