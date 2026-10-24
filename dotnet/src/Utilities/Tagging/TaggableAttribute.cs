namespace AQ.Utilities.Tagging;

/// <summary>Marks a partial entity class as taggable with tags of <paramref name="tagType"/>; the generator adds a Tags collection, the link type and query helpers.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TaggableAttribute(Type tagType) : Attribute
{
    public Type TagType { get; } = tagType;

    /// <summary>Name used for the link type, its foreign key and discriminator; defaults to the class name. Set it when two taggable classes share a name.</summary>
    public string? Name { get; set; }
}
