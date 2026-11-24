namespace AQ.Utilities.Attachments;

public sealed class AttachmentDescriptor(
    string entityTypeName,
    Type entityType,
    Type attachmentType,
    Type baseAttachmentType,
    string foreignKeyPropertyName,
    string navigationPropertyName,
    AttachmentLimits limits,
    AttachmentFactory createLinked,
    AttachmentFactory createPending,
    bool cascadeDelete = true)
{
    public string EntityTypeName { get; } = entityTypeName;
    public Type EntityType { get; } = entityType;

    /// <summary>The generated attachment class of this entity.</summary>
    public Type AttachmentType { get; } = attachmentType;

    /// <summary>The app's attachment type the generated class derives from; all attachments of one base type share a table.</summary>
    public Type BaseAttachmentType { get; } = baseAttachmentType;

    public string ForeignKeyPropertyName { get; } = foreignKeyPropertyName;
    public string NavigationPropertyName { get; } = navigationPropertyName;
    public AttachmentLimits Limits { get; } = limits;

    /// <summary>Creates a linked attachment for the record with the given id.</summary>
    public AttachmentFactory CreateLinked { get; } = createLinked;

    /// <summary>Creates a pending attachment for the record with the given id; the record need not exist.</summary>
    public AttachmentFactory CreatePending { get; } = createPending;

    /// <summary>False when the app removes the attachments before it removes the record, because the database does not.</summary>
    public bool CascadeDelete { get; } = cascadeDelete;
}

/// <summary>Creates an attachment of a record from its id, when the record itself is not at hand.</summary>
public delegate IAttachment AttachmentFactory(Guid ownerId, string fileName, long size, string? contentType, DateTimeOffset? fileModifiedAt);
