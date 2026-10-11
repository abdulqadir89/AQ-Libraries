using AQ.Utilities.EntityData;

namespace AQ.Utilities.Snapshots;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class SnapshotAttribute : EntityDataMemberAttribute
{
}
