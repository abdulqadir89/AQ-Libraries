using AQ.Entities;

namespace AQ.Utilities.Attachments;

/// <summary>
/// Base of an app's attachment type. The uploader and time are the audit creator and creation time.
/// The generated per-record attachment class derives from the app's type and adds the optional link to the record:
/// a file uploaded before its record exists is pending (no link) until the record is saved and the attachment is linked.
/// </summary>
public abstract class Attachment<TPrincipal> : Entity<TPrincipal>, IAttachment where TPrincipal : class
{
    public Guid OwnerId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string? ContentType { get; private set; }
    public DateTimeOffset? FileModifiedAt { get; private set; }
    public bool IsPending { get; private set; }

    protected Attachment() { }

    /// <summary>Sets the file. The generated create methods call this after checking the limits.</summary>
    protected void Initialize(Guid ownerId, bool isPending, string fileName, long size, string? contentType, DateTimeOffset? fileModifiedAt)
    {
        // RULE ATT-2
        ValidateName(fileName);
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        OwnerId = ownerId;
        IsPending = isPending;
        FileName = fileName;
        Size = size;
        ContentType = contentType;
        FileModifiedAt = fileModifiedAt;
    }

    public abstract void Link();

    public abstract void Replace(long size, string? contentType, DateTimeOffset? fileModifiedAt);

    /// <summary>Records a new version of the same file. The generated <see cref="Replace"/> calls this after checking the limits.</summary>
    protected void ReplaceFile(long size, string? contentType, DateTimeOffset? fileModifiedAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        Size = size;
        ContentType = contentType;
        FileModifiedAt = fileModifiedAt;
    }

    protected void MarkLinked() => IsPending = false;

    private static void ValidateName(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        if (fileName.Length is 0 or > 260 || fileName.Any(c => c is '/' or '\\' || char.IsControl(c)) || fileName is "." or "..")
        {
            throw new ArgumentException("The file name is empty, too long or contains a path.", nameof(fileName));
        }
    }
}
