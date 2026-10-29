namespace AQ.Utilities.Commenting;

/// <summary>Marks an entity class as open to threaded comments of <paramref name="commentType"/>; the generator adds a comment class linked to it, query helpers and a registration.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CommentableAttribute(Type commentType) : Attribute
{
    public Type CommentType { get; } = commentType;

    /// <summary>Name used for the generated comment type, its foreign key and discriminator; defaults to the class name. Set it when two commentable classes share a name.</summary>
    public string? Name { get; set; }

    /// <summary>How many levels of replies a comment may have: 1 allows replies to top-level comments only. 0, the default, sets no limit.</summary>
    public int MaxDepth { get; set; }
}
