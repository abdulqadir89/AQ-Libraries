using AQ.EntityFramework.Tests.Generated;
using AQ.EntityFramework.Versioning;
using AQ.Events.Domain;
using AQ.Utilities.Versioning;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Tests.Support;

public sealed class VersionTestContext(DbContextOptions<VersionTestContext> options) : DbContext(options)
{
    public DbSet<TestUser> Users => Set<TestUser>();
    public DbSet<TestFolder> Folders => Set<TestFolder>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<EntityVersion<TestUser>> EntityVersions => Set<EntityVersion<TestUser>>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<ContractVersion> ContractVersions => Set<ContractVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<IDomainEvent>();
        AQGeneratedRegistrations.EnsureRegistered();
        modelBuilder.ApplyEntityVersioning<TestUser>();
    }
}

public sealed class VersionTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public VersionTestDatabase()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public VersionTestContext CreateContext() =>
        new(new DbContextOptionsBuilder<VersionTestContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
