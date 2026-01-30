namespace AQ.DataSeeding.Types;

/// <summary>
/// Marker interface for migration data seeders.
/// Implement IDataSeeder&lt;IMigrationSeeder&gt; to create a migration data seeder.
/// </summary>
public interface IMigrationSeeder : ISeederType
{
}
