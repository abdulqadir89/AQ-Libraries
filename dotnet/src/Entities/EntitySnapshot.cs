namespace AQ.Entities;

public abstract class EntitySnapshot : Entity
{
    public Guid EntityId { get; protected set; }
    public int Version { get; protected set; }
    public string Data { get; protected set; } = string.Empty;

    protected EntitySnapshot() { }
}
