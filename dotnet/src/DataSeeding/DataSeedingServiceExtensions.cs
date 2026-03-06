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
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for seeders. If none provided, uses calling assembly.</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDataSeeders<TSeederType>(
        this IServiceCollection services,
        params Assembly[] assemblies)
        where TSeederType : ISeederType
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

        services.AddScoped<DataSeedingService<TSeederType>>();
        
        return services;
    }

    /// <summary>
    /// Registers all test data seeders from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for seeders. If none provided, uses calling assembly.</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTestDataSeeders(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddDataSeeders<Types.ITestDataSeeder>(assemblies);
    }

    /// <summary>
    /// Registers all configuration data seeders from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for seeders. If none provided, uses calling assembly.</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddConfigurationSeeders(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddDataSeeders<Types.IConfigurationSeeder>(assemblies);
    }

    /// <summary>
    /// Registers all migration data seeders from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for seeders. If none provided, uses calling assembly.</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMigrationSeeders(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddDataSeeders<Types.IMigrationSeeder>(assemblies);
    }

    /// <summary>
    /// Registers all baseline data seeders from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for seeders. If none provided, uses calling assembly.</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddBaselineSeeders(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddDataSeeders<Types.IBaselineSeeder>(assemblies);
    }
}
