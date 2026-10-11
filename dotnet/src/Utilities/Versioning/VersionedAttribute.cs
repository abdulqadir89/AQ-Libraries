using AQ.Utilities.EntityData;

namespace AQ.Utilities.Versioning;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class VersionedAttribute : EntityDataMemberAttribute
{
}
