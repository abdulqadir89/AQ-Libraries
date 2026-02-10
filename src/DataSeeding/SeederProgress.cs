namespace AQ.DataSeeding;

/// <summary>
/// Represents the progress of a seeder execution.
/// </summary>
public record SeederProgress(
    string SeederName,
    int CurrentBatch,
    int TotalBatches,
    int ItemsProcessed,
    bool IsComplete,
    bool IsSuccess,
    string? ErrorMessage,
    TimeSpan Duration);
