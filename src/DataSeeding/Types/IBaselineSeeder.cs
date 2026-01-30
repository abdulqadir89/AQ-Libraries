namespace AQ.DataSeeding.Types;

/// <summary>
/// Marker interface for baseline data seeders.
/// Implement IDataSeeder&lt;IBaselineSeeder&gt; to create a baseline data seeder.
/// Baseline data represents essential, non-changing data required for application functionality.
/// </summary>
public interface IBaselineSeeder : ISeederType
{
}
