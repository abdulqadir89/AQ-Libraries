namespace AQ.Utilities.Attachments;

// TODO-012: replaces AttachmentLimitAttribute, IHasAttachments and PendingAttachment; consuming apps must migrate before this reaches main
/// <summary>Marks an entity class as open to file attachments of <paramref name="attachmentType"/>; the generator adds an attachment class linked to it, query helpers and a registration.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AttachableAttribute(Type attachmentType) : Attribute
{
    public Type AttachmentType { get; } = attachmentType;

    /// <summary>Name used for the generated attachment type, its foreign key and discriminator; defaults to the class name. Set it when two attachable classes share a name.</summary>
    public string? Name { get; set; }

    /// <summary>Maximum number of attachments of one record. 0, the default, sets no limit; the app checks it with <see cref="AttachmentLimits.EnsureCountAllowed"/> because the count needs the database.</summary>
    public int MaxCount { get; set; }

    /// <summary>Maximum size in bytes of one file. 0, the default, sets no limit.</summary>
    public long MaxFileSizeBytes { get; set; }

    /// <summary>Content type presets allowed, combined with <see cref="ExtraContentTypes"/>. <see cref="AttachmentContentKind.None"/> with no extra types allows any content type.</summary>
    public AttachmentContentKind ContentKind { get; set; }

    /// <summary>Content types allowed on top of <see cref="ContentKind"/>.</summary>
    public string[]? ExtraContentTypes { get; set; }

    /// <summary>File extensions allowed, with the leading dot and in lower case, for example ".pdf". Unset allows any extension.</summary>
    public string[]? AllowedExtensions { get; set; }

    /// <summary>Whether the database removes the attachments with the record. Set it to false on a record whose child records are attachable too (SQL Server refuses two cascade paths to the shared table); the app then removes that record's attachments before it removes the record.</summary>
    public bool CascadeDelete { get; set; } = true;
}
