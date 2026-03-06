namespace AQ.DataSeeding;

/// <summary>
/// Interface for data seeders that support batch processing.
/// Implement this interface when dealing with large datasets that should be processed in batches.
/// </summary>
/// <typeparam name="TSeederType">The seeder type marker interface</typeparam>
public interface IBatchDataSeeder<TSeederType> : IDataSeeder<TSeederType>
    where TSeederType : ISeederType
{
    /// <summary>
    /// Gets the total number of batches for this seeder.
    /// </summary>
    int GetBatchCount();

    /// <summary>
    /// Seeds a specific batch of data.
    /// </summary>
    /// <param name="batchNumber">The batch number to seed (1-based)</param>
    /// <returns>The number of items processed in this batch</returns>
    Task<int> SeedBatchAsync(int batchNumber);
}
