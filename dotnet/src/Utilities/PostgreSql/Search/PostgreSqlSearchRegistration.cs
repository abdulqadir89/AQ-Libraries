using Microsoft.Extensions.DependencyInjection;

namespace AQ.Utilities.Search.PostgreSql;

public static class PostgreSqlSearchRegistration
{
    /// <summary>
    /// Registers <see cref="PostgreSqlSearchDialect"/> as the ambient <see cref="SearchDialect.Current"/>.
    /// Call once at startup. Remember to also call
    /// <see cref="PostgreSqlSearchModelBuilderExtensions.AddPostgreSqlSearchExtensions"/> in your
    /// DbContext's OnModelCreating, and to run the extension-creating migration.
    /// </summary>
    public static void Register() => SearchDialect.Current = new PostgreSqlSearchDialect();

    public static IServiceCollection AddPostgreSqlSearch(this IServiceCollection services)
    {
        Register();
        return services;
    }
}
