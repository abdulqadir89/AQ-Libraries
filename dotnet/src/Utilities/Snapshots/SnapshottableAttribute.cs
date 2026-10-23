namespace AQ.Utilities.Snapshots;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SnapshottableAttribute : Attribute
{
    public bool IncludeId { get; set; }
    public string? SnapshotTypeName { get; set; }

    /// <summary>Snapshots to keep per entity when pruning; 0 means no limit.</summary>
    public int KeepLast { get; set; }

    /// <summary>Days a snapshot is kept when pruning; 0 means no limit.</summary>
    public int MaxAgeDays { get; set; }
}
