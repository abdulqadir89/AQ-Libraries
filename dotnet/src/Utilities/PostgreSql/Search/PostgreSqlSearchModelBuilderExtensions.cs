using Microsoft.EntityFrameworkCore;

namespace AQ.Utilities.Search.PostgreSql;

/// <summary>
/// Registers the PostgreSQL extensions required by <see cref="PostgreSqlSearchDialect"/>.
/// Requires the target database role to have CREATE privilege (or the extensions
/// pre-created by a DBA) — consumer's migration responsibility.
/// </summary>
public static class PostgreSqlSearchModelBuilderExtensions
{
    public static ModelBuilder AddPostgreSqlSearchExtensions(this ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasPostgresExtension("fuzzystrmatch");
        return modelBuilder;
    }
}
