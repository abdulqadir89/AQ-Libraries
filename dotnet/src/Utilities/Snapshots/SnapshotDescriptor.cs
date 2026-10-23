namespace AQ.Utilities.Snapshots;

public sealed class SnapshotDescriptor(
    string entityTypeName,
    Type entityType,
    Type snapshotType,
    string foreignKeyPropertyName,
    string navigationPropertyName,
    int keepLast,
    int maxAgeDays,
    Func<Guid, int, string, EntitySnapshot> factory)
{
    public string EntityTypeName { get; } = entityTypeName;
    public Type EntityType { get; } = entityType;
    public Type SnapshotType { get; } = snapshotType;
    public string ForeignKeyPropertyName { get; } = foreignKeyPropertyName;
    public string NavigationPropertyName { get; } = navigationPropertyName;
    public int KeepLast { get; } = keepLast;
    public int MaxAgeDays { get; } = maxAgeDays;
    public Func<Guid, int, string, EntitySnapshot> Factory { get; } = factory;
}
