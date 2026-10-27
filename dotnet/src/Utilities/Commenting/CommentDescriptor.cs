namespace AQ.Utilities.Commenting;

public sealed class CommentDescriptor(
    string entityTypeName,
    Type entityType,
    Type commentType,
    Type baseCommentType,
    string foreignKeyPropertyName,
    string navigationPropertyName)
{
    public string EntityTypeName { get; } = entityTypeName;
    public Type EntityType { get; } = entityType;

    /// <summary>The generated comment class of this entity.</summary>
    public Type CommentType { get; } = commentType;

    /// <summary>The app's comment type the generated class derives from; all comments of one base type share a table.</summary>
    public Type BaseCommentType { get; } = baseCommentType;

    public string ForeignKeyPropertyName { get; } = foreignKeyPropertyName;
    public string NavigationPropertyName { get; } = navigationPropertyName;
}
