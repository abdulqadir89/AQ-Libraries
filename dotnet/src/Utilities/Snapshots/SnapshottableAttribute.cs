namespace AQ.Utilities.Snapshots;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SnapshottableAttribute : Attribute
{
    public bool IncludeId { get; set; }
    public string? SnapshotTypeName { get; set; }
}
