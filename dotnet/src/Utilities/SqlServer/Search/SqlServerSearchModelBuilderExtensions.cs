using Microsoft.EntityFrameworkCore;

namespace AQ.Utilities.Search.SqlServer;

/// <summary>
/// Registers the SQL Server built-in functions used by <see cref="SqlServerSearchDialect"/>
/// so EF Core can translate calls to them.
/// </summary>
public static class SqlServerSearchModelBuilderExtensions
{
    public static ModelBuilder AddSqlServerSearchFunctions(this ModelBuilder modelBuilder)
    {
        modelBuilder.HasDbFunction(() => SqlServerSearchFunctions.Difference(default!, default!))
            .HasName("DIFFERENCE")
            .IsBuiltIn();

        modelBuilder.HasDbFunction(() => SqlServerSearchFunctions.Soundex(default!))
            .HasName("SOUNDEX")
            .IsBuiltIn();

        return modelBuilder;
    }
}
