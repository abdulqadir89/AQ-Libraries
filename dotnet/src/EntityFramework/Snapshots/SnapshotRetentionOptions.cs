namespace AQ.EntityFramework.Snapshots;

public sealed class SnapshotRetentionOptions
{
    public int BatchSize { get; set; } = 1000;

    /// <summary>Per entity type name; a value set here replaces the one on the entity's snapshot attribute, 0 clears that limit.</summary>
    public Dictionary<string, SnapshotRetention> Retention { get; set; } = new(StringComparer.Ordinal);
}

public sealed class SnapshotRetention
{
    public int? KeepLast { get; set; }
    public int? MaxAgeDays { get; set; }
}
