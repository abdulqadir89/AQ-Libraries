using System.Collections.Concurrent;

namespace AQ.Utilities.Snapshots;

public static class SnapshotRegistry
{
    private static readonly ConcurrentDictionary<Type, Func<Guid, int, string, EntitySnapshot>> Factories = new();

    public static void Register(Type entityType, Func<Guid, int, string, EntitySnapshot> factory)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(factory);
        Factories[entityType] = factory;
    }

    // RULE VSN-2
    public static bool TryGetFactory(Type runtimeType, out Func<Guid, int, string, EntitySnapshot> factory)
    {
        ArgumentNullException.ThrowIfNull(runtimeType);
        for (var current = runtimeType; current is not null; current = current.BaseType)
        {
            if (Factories.TryGetValue(current, out var found))
            {
                factory = found;
                return true;
            }
        }

        factory = null!;
        return false;
    }
}
