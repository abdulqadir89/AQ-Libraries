using AQ.Abstractions;

namespace AQ.Entities;

public class SearchIndex : IEntity
{
    public Guid Id { get; set; }

    // Entity type + FK
    public string EntityType { get; set; } = default!;
    public Guid EntityId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;

    // Sync tracking
    public bool IsSync { get; set; } = false;
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}
