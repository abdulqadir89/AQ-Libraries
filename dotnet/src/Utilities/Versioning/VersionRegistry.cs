using System.Collections.Concurrent;

namespace AQ.Utilities.Versioning;

public static class VersionRegistry
{
    private static readonly ConcurrentDictionary<string, VersionDescriptor> ByName = new();
    private static readonly ConcurrentDictionary<Type, VersionDescriptor> ByType = new();

    public static IReadOnlyCollection<VersionDescriptor> All => ByName.Values.OrderBy(d => d.EntityTypeName, StringComparer.Ordinal).ToList();

    public static void Register(VersionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ByName[descriptor.EntityTypeName] = descriptor;
        ByType[descriptor.EntityType] = descriptor;
        ByType[descriptor.VersionType] = descriptor;
    }

    // RULE VSN-2
    public static VersionDescriptor? TryGet(Type runtimeType)
    {
        ArgumentNullException.ThrowIfNull(runtimeType);
        for (var current = runtimeType; current is not null; current = current.BaseType)
        {
            if (ByType.TryGetValue(current, out var descriptor))
            {
                return descriptor;
            }
        }

        return null;
    }

    public static VersionDescriptor? TryGet(string entityTypeName) =>
        ByName.TryGetValue(entityTypeName, out var descriptor) ? descriptor : null;
}
