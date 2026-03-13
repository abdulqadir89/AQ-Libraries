namespace AQ.DataSeeding;

/// <summary>
/// Marks a seeder to be skipped by <see cref="DataSeedingService{TSeederType}"/>.
/// Any seeder that depends on a skipped seeder is also skipped automatically.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SkipSeedingAttribute : Attribute
{
}