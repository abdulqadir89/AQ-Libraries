namespace AQ.Utilities.Search.SqlServer;

/// <summary>
/// CLR stubs mapped to built-in SQL Server T-SQL functions via
/// <see cref="SqlServerSearchModelBuilderExtensions.AddSqlServerSearchFunctions"/>.
/// Never call these directly outside a translated LINQ expression — they only
/// have meaning when evaluated by the database.
/// </summary>
public static class SqlServerSearchFunctions
{
    public static int Difference(string a, string b)
        => throw new NotSupportedException("SqlServerSearchFunctions.Difference is server-side only. Call AddSqlServerSearchFunctions() in OnModelCreating.");

    public static string Soundex(string value)
        => throw new NotSupportedException("SqlServerSearchFunctions.Soundex is server-side only. Call AddSqlServerSearchFunctions() in OnModelCreating.");
}
