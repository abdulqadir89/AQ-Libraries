namespace AQ.Utilities.Attachments;

/// <summary>
/// Decorates an entity class to declare attachment restrictions for that entity type.
/// Resolve with <see cref="AttachmentLimitResolver"/>. Any property left unset falls back
/// to the application's global attachment defaults.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class AttachmentLimitAttribute : Attribute
{
    /// <summary>
    /// Maximum number of attachments allowed for the entity. 0 (default) means no count limit.
    /// </summary>
    public int MaxCount { get; init; }

    /// <summary>
    /// Maximum size in bytes for a single attachment. 0 (default) falls back to the global default.
    /// </summary>
    public long MaxFileSizeBytes { get; init; }

    /// <summary>
    /// Named MIME presets allowed for this entity. Combine with <see cref="ExtraContentTypes"/>
    /// for one-off MIME types not covered by a preset. Leave as <see cref="AttachmentContentKind.None"/>
    /// (with no <see cref="ExtraContentTypes"/>) to fall back to the global content-type allowlist.
    /// </summary>
    public AttachmentContentKind ContentKind { get; init; }

    /// <summary>
    /// Additional MIME types allowed alongside <see cref="ContentKind"/>, for types not covered by a preset.
    /// </summary>
    public string[]? ExtraContentTypes { get; init; }
}
