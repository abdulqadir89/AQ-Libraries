using AQ.Entities;

namespace AQ.Utilities.Attachments;

public interface IAttachment : IEntity
{
    /// <summary>The id of the record the file belongs to, also while it is pending and that record does not exist yet.</summary>
    Guid OwnerId { get; }

    string FileName { get; }
    long Size { get; }
    string? ContentType { get; }
    DateTimeOffset? FileModifiedAt { get; }

    /// <summary>True until the record exists and the attachment is linked to it.</summary>
    bool IsPending { get; }

    /// <summary>Links a pending attachment to the record whose id it waits for.</summary>
    void Link();

    /// <summary>Records a new version of the same file; the new size, content type and extension must pass the limits.</summary>
    void Replace(long size, string? contentType, DateTimeOffset? fileModifiedAt);
}
