namespace AQ.AI;

public interface IVectorSearchService
{
    Task<IEnumerable<SearchResult>> SearchAsync(string query, int k = 25, CancellationToken cancellationToken = default);
    Task<IEnumerable<SearchResult>> SearchAsync(string query, int k = 25, double vectorDistanceFilter = 0.4, CancellationToken cancellationToken = default);
    Task<VectorDatabaseResetResult> ResetVectorDatabaseAsync(CancellationToken cancellationToken = default);
}

public record SearchResult
{
    public required string EntityType { get; init; }
    public required Guid EntityId { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public required double Score { get; set; }

    public string ReadableEntityName =>
        string.IsNullOrWhiteSpace(EntityType)
            ? string.Empty
            : System.Text.RegularExpressions.Regex.Replace(EntityType, "([a-z])([A-Z])", "$1 $2");
}

public record VectorDatabaseResetResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public int TotalRecordsProcessed { get; init; }
    public int RecordsUpserted { get; init; }
    public int Errors { get; init; }
    public double DurationMs { get; init; }
    public Dictionary<string, int> EntitiesByType { get; init; } = new();
}