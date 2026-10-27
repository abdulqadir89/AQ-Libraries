using System.Collections.Concurrent;

namespace AQ.Utilities.Commenting;

public static class CommentRegistry
{
    private static readonly ConcurrentDictionary<string, CommentDescriptor> ByName = new();

    public static IReadOnlyCollection<CommentDescriptor> All => ByName.Values.OrderBy(d => d.EntityTypeName, StringComparer.Ordinal).ToList();

    public static void Register(CommentDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ByName[descriptor.EntityTypeName] = descriptor;
    }
}
