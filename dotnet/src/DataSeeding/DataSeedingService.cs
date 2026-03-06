using AQ.Utilities.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AQ.DataSeeding;

/// <summary>
/// Generic data seeding service that handles dependency resolution and execution order for a specific seeder type.
/// DbContext is passed as a method parameter to support multiple contexts.
/// </summary>
/// <typeparam name="TSeederType">The seeder type to process (ITestDataSeeder, IConfigurationSeeder, etc.)</typeparam>
public class DataSeedingService<TSeederType>
    where TSeederType : ISeederType
{
    private readonly Dictionary<Type, IDataSeeder<TSeederType>> _seederMap;
    private readonly ILogger<DataSeedingService<TSeederType>> _logger;
    private readonly string _seederTypeName;

    public DataSeedingService(
        IEnumerable<IDataSeeder<TSeederType>> seeders,
        ILogger<DataSeedingService<TSeederType>> logger)
    {
        _seederMap = seeders.ToDictionary(s => s.GetType());
        _logger = logger;
        _seederTypeName = typeof(TSeederType).Name;
    }

    /// <summary>
    /// Seeds all registered seeders of this type in dependency order with real-time progress reporting.
    /// </summary>
    /// <param name="onProgress">Optional callback to receive progress updates in real-time</param>
    public async Task<Result> SeedAllAsync(Func<SeederProgress, Task>? onProgress = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var errors = new List<string>();

        try
        {
            _logger.LogInformation("Starting {SeederType} seeding process...", _seederTypeName);
            var ordered = ResolveOrder();
            _logger.LogInformation("Resolved seeding order for {Count} {SeederType} seeders", ordered.Count, _seederTypeName);

            for (int i = 0; i < ordered.Count; i++)
            {
                var seeder = ordered[i];
                var seederName = seeder.GetType().Name;
                _logger.LogInformation("[{Index}/{Total}] Starting {SeederType} seeder: {SeederName}",
                    i + 1, ordered.Count, _seederTypeName, seederName);

                try
                {
                    // Check if seeder supports batch processing
                    if (seeder is IBatchDataSeeder<TSeederType> batchSeeder)
                    {
                        var totalBatches = batchSeeder.GetBatchCount();
                        _logger.LogInformation("Seeder {SeederName} will process {TotalBatches} batches",
                            seederName, totalBatches);

                        for (int batch = 1; batch <= totalBatches; batch++)
                        {
                            var batchStopwatch = System.Diagnostics.Stopwatch.StartNew();
                            try
                            {
                                var itemsProcessed = await batchSeeder.SeedBatchAsync(batch);
                                batchStopwatch.Stop();

                                var progress = new SeederProgress(
                                    seederName,
                                    batch,
                                    totalBatches,
                                    itemsProcessed,
                                    batch == totalBatches,
                                    true,
                                    null,
                                    batchStopwatch.Elapsed);

                                _logger.LogInformation(
                                    "✓ [{SeederName}] Batch {Batch}/{TotalBatches} completed - " +
                                    "Items: {ItemsProcessed}, Duration: {Duration:mm\\:ss\\.fff}",
                                    seederName, batch, totalBatches, itemsProcessed, batchStopwatch.Elapsed);

                                if (onProgress != null)
                                    await onProgress(progress);
                            }
                            catch (Exception ex)
                            {
                                batchStopwatch.Stop();
                                var errorMsg = $"Batch {batch}/{totalBatches} failed: {ex.Message}";
                                errors.Add($"{seederName}: {errorMsg}");

                                var progress = new SeederProgress(
                                    seederName,
                                    batch,
                                    totalBatches,
                                    0,
                                    false,
                                    false,
                                    errorMsg,
                                    batchStopwatch.Elapsed);

                                _logger.LogError(ex,
                                    "✗ [{SeederName}] Batch {Batch}/{TotalBatches} FAILED after {Duration:mm\\:ss\\.fff}",
                                    seederName, batch, totalBatches, batchStopwatch.Elapsed);

                                if (onProgress != null)
                                    await onProgress(progress);

                                throw;
                            }
                        }

                        _logger.LogInformation("✓ [{SeederName}] All batches completed successfully", seederName);
                    }
                    else
                    {
                        // Non-batch seeder - treat as single operation
                        var seederStopwatch = System.Diagnostics.Stopwatch.StartNew();
                        try
                        {
                            await seeder.SeedAsync();
                            seederStopwatch.Stop();

                            var progress = new SeederProgress(
                                seederName,
                                1,
                                1,
                                0,
                                true,
                                true,
                                null,
                                seederStopwatch.Elapsed);

                            _logger.LogInformation("✓ [{SeederName}] Completed in {Duration:mm\\:ss\\.fff}",
                                seederName, seederStopwatch.Elapsed);

                            if (onProgress != null)
                                await onProgress(progress);
                        }
                        catch (Exception ex)
                        {
                            seederStopwatch.Stop();
                            var errorMsg = ex.Message;
                            errors.Add($"{seederName}: {errorMsg}");

                            var progress = new SeederProgress(
                                seederName,
                                1,
                                1,
                                0,
                                true,
                                false,
                                errorMsg,
                                seederStopwatch.Elapsed);

                            _logger.LogError(ex, "✗ [{SeederName}] FAILED after {Duration:mm\\:ss\\.fff}",
                                seederName, seederStopwatch.Elapsed);

                            if (onProgress != null)
                                await onProgress(progress);

                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Critical error in seeder: {SeederName}", seederName);
                    throw;
                }
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "✓ {SeederType} seeding completed successfully in {Duration:mm\\:ss\\.fff}",
                _seederTypeName, stopwatch.Elapsed);

            return Result.Success();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, 
                "❌ {SeederType} seeding FAILED after {Duration:mm\\:ss\\.fff}",
                _seederTypeName, stopwatch.Elapsed);

            return Result.Failure(new Error(
                ErrorType.General,
                "SeedingFailed",
                $"{_seederTypeName} seeding failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Drops the database.
    /// </summary>
    public async Task DropDatabaseAsync(DbContext dbContext)
    {
        try
        {
            _logger.LogInformation("Dropping database...");
            await dbContext.Database.EnsureDeletedAsync();
            _logger.LogInformation("Database dropped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while dropping database");
            throw;
        }
    }

    /// <summary>
    /// Applies pending migrations to the database.
    /// </summary>
    public async Task ApplyMigrationsAsync(DbContext dbContext)
    {
        try
        {
            _logger.LogInformation("Applying database migrations...");
            await dbContext.Database.MigrateAsync();
            _logger.LogInformation("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while applying migrations");
            throw;
        }
    }

    /// <summary>
    /// Drops database, applies migrations, and seeds all data. Useful for testing.
    /// </summary>
    public async Task<Result> ResetAllAsync(DbContext dbContext, Func<SeederProgress, Task>? onProgress = null)
    {
        await DropDatabaseAsync(dbContext);
        await ApplyMigrationsAsync(dbContext);
        return await SeedAllAsync(onProgress);
    }

    /// <summary>
    /// Gets the number of registered seeders for this type.
    /// </summary>
    public int GetSeederCount() => _seederMap.Count;

    private List<IDataSeeder<TSeederType>> ResolveOrder()
    {
        var resolved = new List<IDataSeeder<TSeederType>>();
        var visited = new HashSet<Type>();

        // Get all seeders sorted by priority
        var sortedSeeders = _seederMap.Values.OrderBy(s => s.Priority).ToList();

        foreach (var seeder in sortedSeeders)
        {
            Visit(seeder.GetType(), visited, resolved);
        }

        return resolved;
    }

    private void Visit(Type type, HashSet<Type> visited, List<IDataSeeder<TSeederType>> resolved)
    {
        if (visited.Contains(type)) return;

        visited.Add(type);

        var seeder = _seederMap[type];
        
        // Get dependencies and sort by priority
        var dependencies = seeder.Dependencies
            .Select(dep => new { Type = dep, Seeder = _seederMap.TryGetValue(dep, out var s) ? s : null })
            .Where(x => x.Seeder != null)
            .OrderBy(x => x.Seeder!.Priority);

        foreach (var dep in dependencies)
        {
            if (!_seederMap.ContainsKey(dep.Type))
            {
                throw new InvalidOperationException(
                    $"Dependency {dep.Type.Name} for {type.Name} not registered as {_seederTypeName}");
            }
            Visit(dep.Type, visited, resolved);
        }

        resolved.Add(seeder);
    }
}
