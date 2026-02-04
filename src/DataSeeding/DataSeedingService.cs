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
    /// Seeds all registered seeders of this type in dependency order.
    /// </summary>
    public async Task SeedAllAsync()
    {
        try
        {
            _logger.LogInformation("Starting {SeederType} seeding process...", _seederTypeName);
            var ordered = ResolveOrder();
            _logger.LogInformation("Resolved seeding order for {Count} {SeederType} seeders", ordered.Count, _seederTypeName);

            for (int i = 0; i < ordered.Count; i++)
            {
                var seeder = ordered[i];
                var seederName = seeder.GetType().Name;
                _logger.LogInformation("Running {SeederType} seeder {Index}/{Total}: {SeederName}",
                    _seederTypeName, i + 1, ordered.Count, seederName);

                try
                {
                    await seeder.SeedAsync();
                    _logger.LogInformation("Successfully completed {SeederType} seeder: {SeederName}",
                        _seederTypeName, seederName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to run {SeederType} seeder: {SeederName}",
                        _seederTypeName, seederName);
                    throw;
                }
            }

            _logger.LogInformation("{SeederType} seeding completed successfully", _seederTypeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during {SeederType} seeding", _seederTypeName);
            throw;
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
    public async Task ResetAllAsync(DbContext dbContext)
    {
        await DropDatabaseAsync(dbContext);
        await ApplyMigrationsAsync(dbContext);
        await SeedAllAsync();
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
