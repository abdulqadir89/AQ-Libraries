namespace AQ.Entities;

public interface ISnapshottable
{
    EntitySnapshot CreateSnapshot(int version);
    Dictionary<string, object?> GetSnapshotData();
}

public interface ISnapshottable<TSnapshot> : ISnapshottable where TSnapshot : EntitySnapshot
{
    new TSnapshot CreateSnapshot(int version);

    EntitySnapshot ISnapshottable.CreateSnapshot(int version) => CreateSnapshot(version);
}
