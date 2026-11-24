namespace AQ.Utilities.Attachments;

/// <summary>The limits an attachable class declares. A limit of 0 or an empty list means none.</summary>
public sealed class AttachmentLimits(
    int maxCount,
    long maxFileSizeBytes,
    AttachmentContentKind contentKind,
    string[]? extraContentTypes,
    string[]? allowedExtensions)
{
    private readonly HashSet<string> _contentTypes = new(
        AttachmentContentTypes.Resolve(contentKind).Concat(extraContentTypes ?? []),
        StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _extensions = new(allowedExtensions ?? [], StringComparer.OrdinalIgnoreCase);

    public int MaxCount { get; } = maxCount;
    public long MaxFileSizeBytes { get; } = maxFileSizeBytes;
    public IReadOnlyCollection<string> AllowedContentTypes => _contentTypes;
    public IReadOnlyCollection<string> AllowedExtensions => _extensions;

    // RULE ATT-1
    public void EnsureFileAllowed(string fileName, long size, string? contentType)
    {
        if (MaxFileSizeBytes > 0 && size > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"A file cannot be larger than {MaxFileSizeBytes} bytes.");
        }

        if (_contentTypes.Count > 0 && (string.IsNullOrWhiteSpace(contentType) || !_contentTypes.Contains(contentType)))
        {
            throw new InvalidOperationException($"The content type '{contentType}' is not allowed.");
        }

        if (_extensions.Count > 0 && !_extensions.Contains(Path.GetExtension(fileName)))
        {
            throw new InvalidOperationException($"The file extension '{Path.GetExtension(fileName)}' is not allowed.");
        }
    }

    // RULE ATT-1
    public void EnsureCountAllowed(int existingCount)
    {
        if (MaxCount > 0 && existingCount >= MaxCount)
        {
            throw new InvalidOperationException($"A record cannot have more than {MaxCount} attachment(s).");
        }
    }
}
