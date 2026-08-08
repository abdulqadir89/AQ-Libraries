using Microsoft.Extensions.DependencyInjection;

namespace AQ.Utilities.Search.SqlServer;

public static class SqlServerSearchRegistration
{
    /// <summary>
    /// Registers <see cref="SqlServerSearchDialect"/> as the ambient <see cref="SearchDialect.Current"/>.
    /// Call once at startup. Remember to also call
    /// <see cref="SqlServerSearchModelBuilderExtensions.AddSqlServerSearchFunctions"/> in your
    /// DbContext's OnModelCreating.
    /// </summary>
    public static void Register() => SearchDialect.Current = new SqlServerSearchDialect();

    public static IServiceCollection AddSqlServerSearch(this IServiceCollection services)
    {
        Register();
        return services;
    }
}
