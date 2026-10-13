using AQ.Actors;
using AQ.EntityFramework.Review;
using AQ.EntityFramework.SaveChanges;
using AQ.EntityFramework.Verification;
using AQ.EntityFramework.Tests.Generated;
using AQ.Events.Domain;
using AQ.Events.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Tests.Support;

public sealed class PipelineTestContext(DbContextOptions<PipelineTestContext> options) : DbContext(options)
{
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
            builder.Ignore(t => t.EvaluationCount);
            builder.Ignore(t => t.AddCalls);
            builder.Ignore(t => t.UpdateCalls);
            builder.OwnsOne(t => t.Price);
            builder.HasVerificationState(t => t.Verification);
            builder.HasReviewState(t => t.Review);
        });
    }
}

/// <summary>Plain (non-async-local) actor context so xunit's per-test execution flow cannot lose the actor.</summary>
public sealed class TestActorContext : IActorContext
{
    public Actor? Actor { get; set; }

    public Actor Current => Actor ?? throw new MissingActorException();

    public bool TryGet([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Actor? actor)
    {
        actor = Actor;
        return actor is not null;
    }

    public IDisposable Begin(Actor actor)
    {
        Actor = actor;
        return new Reset(this);
    }

    private sealed class Reset(TestActorContext owner) : IDisposable
    {
        public void Dispose() => owner.Actor = null;
    }
}

public sealed class PipelineTestDatabase : IDisposable
{
    public TestActorContext Actors { get; } = new();

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public PipelineTestDatabase()
    {
        _connection.Open();
        using var context = CreateContext(new AqSavePipelineOptions());
        context.Database.EnsureCreated();
    }

    public PipelineTestContext CreateContext(AqSavePipelineOptions options, Guid? userId = null)
    {
        Actors.Actor = userId is { } id ? new Actor(id, PrincipalKind.Human) : null;
        var context = new PipelineTestContext(new DbContextOptionsBuilder<PipelineTestContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AqSaveChangesInterceptor(options, Actors))
            .Options);
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
