namespace AQ.Utilities.Versioning;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class VersionableAttribute : Attribute
{
    public bool IncludeId { get; set; }
}
