namespace AQ.DataSeeding;

/// <summary>
/// Base interface for all data seeders.
/// Implement this interface along with a seeder type marker interface (ITestDataSeeder, IConfigurationSeeder, etc.)
/// </summary>
/// <typeparam name="TSeederType">The seeder type marker interface (ITestDataSeeder, IConfigurationSeeder, etc.)</typeparam>
public interface IDataSeeder<TSeederType> where TSeederType : ISeederType
{
    /// <summary>
    /// Seed this entity's data.
    /// </summary>
    Task SeedAsync();

    /// <summary>
    /// Other seeders this one depends on. Dependencies must be of the same seeder type.
    /// </summary>
    IEnumerable<Type> Dependencies { get; }

    /// <summary>
    /// Order priority for seeding. Lower numbers run first. Default is 0.
    /// Use this for explicit ordering within the same dependency level.
    /// </summary>
    int Priority => 0;
}
