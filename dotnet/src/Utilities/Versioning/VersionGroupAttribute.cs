namespace AQ.Utilities.Versioning;

/// <summary>Marks a partial entity class as a member of a version group; the generator adds <c>VersionGroupId</c>, <c>Version</c> and <see cref="IVersionGrouped"/>.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class VersionGroupAttribute : Attribute
{
}
