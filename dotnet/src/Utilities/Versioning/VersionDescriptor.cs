namespace AQ.Utilities.Versioning;

public sealed class VersionDescriptor(
    string entityTypeName,
    Type entityType,
    Type versionType,
    string foreignKeyPropertyName,
    string navigationPropertyName,
    Func<Guid, int, string?, string, int?, object> factory,
    Func<IQueryableSource, Guid, IQueryable> query)
{
    public string EntityTypeName { get; } = entityTypeName;
    public Type EntityType { get; } = entityType;
    public Type VersionType { get; } = versionType;
    public string ForeignKeyPropertyName { get; } = foreignKeyPropertyName;
    public string NavigationPropertyName { get; } = navigationPropertyName;
    public Func<Guid, int, string?, string, int?, object> Factory { get; } = factory;
    public Func<IQueryableSource, Guid, IQueryable> Query { get; } = query;
}
