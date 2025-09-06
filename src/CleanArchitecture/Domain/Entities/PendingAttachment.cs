namespace AQ.Domain.Entities;

public class PendingAttachment
{
    public string EntityType { get; private set; } = default!;
    public Guid EntityId { get; private set; } = default!;
    public string FileName { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static PendingAttachment Create(string entityType, Guid entityId, string fileName)
    {
        return new PendingAttachment
        {
            EntityType = entityType,
            EntityId = entityId,
            FileName = fileName
        };
    }

    public static PendingAttachment Create(string entityType, string entityId, string fileName)
    {
        return new PendingAttachment
        {
            EntityType = entityType,
            EntityId = Guid.Parse(entityId),
            FileName = fileName
        };
    }
}
