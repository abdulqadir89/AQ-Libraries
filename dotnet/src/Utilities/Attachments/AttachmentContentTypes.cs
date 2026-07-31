namespace AQ.Utilities.Attachments;

/// <summary>
/// Reusable MIME type groupings for composing <see cref="AttachmentLimitAttribute.ContentTypes"/>.
/// </summary>
public static class AttachmentContentTypes
{
    public static readonly string[] Images =
    [
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp",
        "image/svg+xml",
    ];

    public static readonly string[] Documents =
    [
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain",
    ];

    public static readonly string[] Audio =
    [
        "audio/mpeg",
        "audio/wav",
        "audio/ogg",
    ];

    public static readonly string[] Video =
    [
        "video/mp4",
        "video/webm",
        "video/ogg",
    ];

    /// <summary>
    /// Resolves an <see cref="AttachmentContentKind"/> flag combination to its MIME type list.
    /// </summary>
    public static IEnumerable<string> Resolve(AttachmentContentKind kind)
    {
        if (kind.HasFlag(AttachmentContentKind.Images))
        {
            foreach (var type in Images) yield return type;
        }

        if (kind.HasFlag(AttachmentContentKind.Documents))
        {
            foreach (var type in Documents) yield return type;
        }

        if (kind.HasFlag(AttachmentContentKind.Audio))
        {
            foreach (var type in Audio) yield return type;
        }

        if (kind.HasFlag(AttachmentContentKind.Video))
        {
            foreach (var type in Video) yield return type;
        }
    }
}
