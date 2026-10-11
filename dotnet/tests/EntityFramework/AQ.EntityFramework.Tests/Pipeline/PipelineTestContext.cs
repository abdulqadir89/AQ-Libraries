using AQ.EntityFramework.SaveChanges;
using AQ.EntityFramework.Tests.Generated;
using AQ.Events.Domain;
using AQ.Events.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Tests.Support;

public sealed class PipelineTestContext(DbContextOptions<PipelineTestContext> options) : DbContext(options), ICurrentUserProvider
{
    public Guid? CurrentUserId { get; set; }

    public DbSet<TestUser> Users => Set<TestUser>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketSnapshot> TicketSnapshots => Set<TicketSnapshot>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<IDomainEvent>();
        AQGeneratedRegistrations.EnsureRegistered();
        modelBuilder.Entity<Ticket>(builder =>
        {
            builder.Ignore(t => t.CurrentVerifications);
            builder.OwnsOne(t => t.Price);
        });
    }
}

public sealed class PipelineTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public PipelineTestDatabase()
    {
        _connection.Open();
        using var context = CreateContext(new AqSavePipelineOptions());
        context.Database.EnsureCreated();
    }

    public PipelineTestContext CreateContext(AqSavePipelineOptions options, Guid? userId = null)
    {
        var context = new PipelineTestContext(new DbContextOptionsBuilder<PipelineTestContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AqSaveChangesInterceptor(options))
            .Options)
        {
            CurrentUserId = userId
        };
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
