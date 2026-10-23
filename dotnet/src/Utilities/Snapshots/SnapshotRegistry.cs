using System.Collections.Concurrent;

namespace AQ.Utilities.Snapshots;

public static class SnapshotRegistry
{
    private static readonly ConcurrentDictionary<Type, Func<Guid, int, string, EntitySnapshot>> Factories = new();
    private static readonly ConcurrentDictionary<string, SnapshotDescriptor> ByName = new();

    public static IReadOnlyCollection<SnapshotDescriptor> All => ByName.Values.OrderBy(d => d.EntityTypeName, StringComparer.Ordinal).ToList();

    public static void Register(Type entityType, Func<Guid, int, string, EntitySnapshot> factory)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(factory);
        Factories[entityType] = factory;
    }

    public static void Register(SnapshotDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ByName[descriptor.EntityTypeName] = descriptor;
        Factories[descriptor.EntityType] = descriptor.Factory;
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
