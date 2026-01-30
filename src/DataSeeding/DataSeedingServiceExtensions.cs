using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AQ.DataSeeding;

/// <summary>
/// Extension methods for registering data seeding services.
/// </summary>
public static class DataSeedingServiceExtensions
{
    /// <summary>
    /// Registers all data seeders of a specific type from the specified assemblies.
    /// </summary>
    /// <typeparam name="TSeederType">The seeder type to register (ITestDataSeeder, IConfigurationSeeder, etc.)</typeparam>
    /// <typeparam name="TDbContext">The database context type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for seeders. If none provided, uses calling assembly.</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDataSeeders<TSeederType, TDbContext>(
        this IServiceCollection services,
        params Assembly[] assemblies)
        where TSeederType : ISeederType
        where TDbContext : DbContext
    {
        var assembliesToScan = assemblies.Length > 0 
            ? assemblies 
            : new[] { Assembly.GetCallingAssembly() };

        var seederInterfaceType = typeof(IDataSeeder<TSeederType>);

        foreach (var assembly in assembliesToScan)
        {
            var seederTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && seederInterfaceType.IsAssignableFrom(t))
                .ToList();

            foreach (var seederType in seederTypes)
            {
                services.AddScoped(seederInterfaceType, seederType);
            }
        }

        services.AddScoped<DataSeedingService<TSeederType, TDbContext>>();
        
        return services;
    }

    /// <summary>
    /// Registers all test data seeders from the specified assemblies.
    /// </summary>
    /// <typeparam name="TDbContext">The database context type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for seeders. If none provided, uses calling assembly.</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTestDataSeeders<TDbContext>(
        this IServiceCollection services,
        params Assembly[] assemblies)
        where TDbContext : DbContext
    {
        return services.AddDataSeeders<Types.ITestDataSeeder, TDbContext>(assemblies);
    }

    /// <summary>
    /// Registers all configuration data seeders from the specified assemblies.
    /// </summary>
    /// <typeparam name="TDbContext">The database context type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for seeders. If none provided, uses calling assembly.</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddConfigurationSeeders<TDbContext>(
        this IServiceCollection services,
        params Assembly[] assemblies)
        where TDbContext : DbContext
    {
        return services.AddDataSeeders<Types.IConfigurationSeeder, TDbContext>(assemblies);
    }

    /// <summary>
    /// Registers all migration data seeders from the specified assemblies.
    /// </summary>
    /// <typeparam name="TDbContext">The database context type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for seeders. If none provided, uses calling assembly.</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMigrationSeeders<TDbContext>(
        this IServiceCollection services,
        params Assembly[] assemblies)
        where TDbContext : DbContext
    {
        return services.AddDataSeeders<Types.IMigrationSeeder, TDbContext>(assemblies);
    }
}
