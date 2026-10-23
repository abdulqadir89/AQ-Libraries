using System.Collections.Concurrent;

namespace AQ.Utilities.Tagging;

public static class TagRegistry
{
    private static readonly ConcurrentDictionary<string, TagDescriptor> ByName = new();

    public static IReadOnlyCollection<TagDescriptor> All => ByName.Values.OrderBy(d => d.EntityTypeName, StringComparer.Ordinal).ToList();

    public static void Register(TagDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ByName[descriptor.EntityTypeName] = descriptor;
    }
}
