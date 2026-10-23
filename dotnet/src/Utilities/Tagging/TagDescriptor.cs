namespace AQ.Utilities.Tagging;

public sealed class TagDescriptor(
    string entityTypeName,
    Type entityType,
    Type linkType,
    Type tagType,
    string foreignKeyPropertyName,
    string navigationPropertyName,
    string collectionPropertyName)
{
    public string EntityTypeName { get; } = entityTypeName;
    public Type EntityType { get; } = entityType;
    public Type LinkType { get; } = linkType;
    public Type TagType { get; } = tagType;
    public string ForeignKeyPropertyName { get; } = foreignKeyPropertyName;
    public string NavigationPropertyName { get; } = navigationPropertyName;
    public string CollectionPropertyName { get; } = collectionPropertyName;
}
