using AQ.Actors;
using AQ.EntityFramework.SaveChanges;
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
    public static readonly Guid UserId = Guid.NewGuid();
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly TestActorContext _actors = new() { Actor = new Actor(UserId, PrincipalKind.Human) };

    public VersionTestDatabase()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
        context.Users.Add(new TestUser { Id = UserId });
        context.SaveChanges();
    }

    public VersionTestContext CreateContext() =>
        new(new DbContextOptionsBuilder<VersionTestContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AqSaveChangesInterceptor(new AqSavePipelineOptions { Audit = true }, _actors))
            .Options);

    public void Dispose() => _connection.Dispose();
}
