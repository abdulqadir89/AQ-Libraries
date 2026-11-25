using System.Collections.Concurrent;

namespace AQ.Utilities.Attachments;

public static class AttachmentRegistry
{
    private static readonly ConcurrentDictionary<string, AttachmentDescriptor> ByName = new();

    public static IReadOnlyCollection<AttachmentDescriptor> All => ByName.Values.OrderBy(d => d.EntityTypeName, StringComparer.Ordinal).ToList();

    public static AttachmentDescriptor? Find(string entityTypeName) => ByName.GetValueOrDefault(entityTypeName);

    public static void Register(AttachmentDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ByName[descriptor.EntityTypeName] = descriptor;
    }
}
