namespace AQ.Utilities.EntityData;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public abstract class EntityDataMemberAttribute : Attribute
{
    public string? Name { get; set; }
    public string? Member { get; set; }
    public string[]? Include { get; set; }
    public string[]? Nested { get; set; }
    public bool AsString { get; set; }
    public bool EnumAsNumber { get; set; }
}
