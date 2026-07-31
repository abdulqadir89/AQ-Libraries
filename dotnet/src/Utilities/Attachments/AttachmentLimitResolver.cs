using System.Collections.Concurrent;
using System.Reflection;

namespace AQ.Utilities.Attachments;

public static class AttachmentLimitResolver
{
    private static readonly ConcurrentDictionary<Type, AttachmentLimitAttribute?> LimitByType = new();

    public static AttachmentLimitAttribute? GetLimit(Type entityType) =>
        LimitByType.GetOrAdd(entityType, static t => t.GetCustomAttribute<AttachmentLimitAttribute>());

    /// <summary>
    /// Resolves the effective allowed MIME types for an entity type's <see cref="AttachmentLimitAttribute"/>,
    /// combining its <see cref="AttachmentLimitAttribute.ContentKind"/> presets with any
    /// <see cref="AttachmentLimitAttribute.ExtraContentTypes"/>. Returns null if the entity is undecorated
    /// or declares neither, signalling callers to fall back to the global allowlist.
    /// </summary>
    public static string[]? GetAllowedContentTypes(Type entityType)
    {
        var limit = GetLimit(entityType);
        if (limit is null || (limit.ContentKind == AttachmentContentKind.None && limit.ExtraContentTypes is null))
        {
            return null;
        }

        return [.. AttachmentContentTypes.Resolve(limit.ContentKind), .. limit.ExtraContentTypes ?? []];
    }

    /// <summary>
    /// Resolves the effective max file size in bytes, or null to fall back to the global default
    /// (attribute's <see cref="AttachmentLimitAttribute.MaxFileSizeBytes"/> of 0 means unset).
    /// </summary>
    public static long? GetMaxFileSizeBytes(Type entityType)
    {
        var value = GetLimit(entityType)?.MaxFileSizeBytes ?? 0;
        return value > 0 ? value : null;
    }

    /// <summary>
    /// Resolves the effective max attachment count, or null for no limit
    /// (attribute's <see cref="AttachmentLimitAttribute.MaxCount"/> of 0 means unset).
    /// </summary>
    public static int? GetMaxCount(Type entityType)
    {
        var value = GetLimit(entityType)?.MaxCount ?? 0;
        return value > 0 ? value : null;
    }
}
