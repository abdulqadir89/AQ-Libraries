using AQ.EntityFramework.SaveChanges;
using AQ.EntityFramework.Snapshots;
using AQ.EntityFramework.Tests.Support;
using AQ.Utilities.Snapshots;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Tests.Snapshots;

public class EntitySnapshotTests : IDisposable
{
    private static readonly AqSavePipelineOptions All = new()
    {
        Lifecycle = true,
        GeneratedReferences = true,
        VerificationReset = true,
        Review = true,
        Audit = true,
        Snapshots = true,
        Outbox = true,
    };

    private readonly PipelineTestDatabase _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    private PipelineTestContext CreateContext()
    {
        var context = _database.CreateContext(All, _userId);
        context.Users.Add(new TestUser { Id = _userId });
        context.SaveChanges();
        return context;
    }

    private static SnapshotPruner Pruner(SnapshotRetentionOptions? options = null, int daysAhead = 0) =>
        new(options ?? new SnapshotRetentionOptions(), new FixedTime(DateTimeOffset.UtcNow.AddDays(daysAhead)));

    private static async Task<Invoice> SaveInvoiceVersionsAsync(PipelineTestContext context, int saves)
    {
        var invoice = Invoice.Create("INV-0");
        context.Add(invoice);
        await context.SaveChangesAsync();
        for (var i = 1; i < saves; i++)
        {
            invoice.Rename("INV-" + i);
            await context.SaveChangesAsync();
        }

        return invoice;
    }

    private static Task<List<int>> VersionsAsync(PipelineTestContext context, Guid entityId) =>
        context.Set<EntitySnapshot>().Where(s => s.EntityId == entityId).OrderBy(s => s.Version).Select(s => s.Version).ToListAsync();

    [Fact]
    public void Model_UsesSingleTableWithDiscriminatorAndForeignKeyPerSubtype()
    {
        using var context = CreateContext();

        var root = context.Model.FindEntityType(typeof(EntitySnapshot))!;
        root.GetTableName().Should().Be("EntitySnapshots");
        root.FindDiscriminatorProperty()!.Name.Should().Be("EntityType");
        root.FindDiscriminatorProperty()!.GetMaxLength().Should().Be(100);
        context.Model.FindEntityType(typeof(TicketSnapshot))!.GetTableName().Should().Be("EntitySnapshots");
        context.Model.FindEntityType(typeof(InvoiceSnapshot))!.GetTableName().Should().Be("EntitySnapshots");

        var columns = context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('EntitySnapshots')").ToList();
        columns.Should().Contain(["EntityType", "EntityId", "TicketId", "InvoiceId", "Version", "Data", "CreatedAt"]);
        context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name LIKE '%Snapshot%'").ToList()
            .Should().Equal("EntitySnapshots");
    }

    [Fact]
    public async Task Snapshot_IsDeletedWithItsEntity()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Doomed");
        context.Add(ticket);
        await context.SaveChangesAsync();
        ticket.Rename("Doomed 2");
        await context.SaveChangesAsync();
        (await VersionsAsync(context, ticket.Id)).Should().Equal(0, 1);

        context.Remove(ticket);
        await context.SaveChangesAsync();

        (await VersionsAsync(context, ticket.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Prune_WithNoPolicy_KeepsEverything()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Forever");
        context.Add(ticket);
        await context.SaveChangesAsync();
        ticket.Rename("Forever 2");
        await context.SaveChangesAsync();

        var result = await Pruner().PruneAsync(context);

        result.Should().NotContainKey("Ticket");
        (await VersionsAsync(context, ticket.Id)).Should().Equal(0, 1);
    }

    [Fact]
    public async Task Prune_KeepLastFromAttribute_KeepsNewestSnapshots()
    {
        using var context = CreateContext();
        var invoice = await SaveInvoiceVersionsAsync(context, 5);

        var result = await Pruner().PruneAsync(context);

        result["Invoice"].Should().Be(3);
        (await VersionsAsync(context, invoice.Id)).Should().Equal(3, 4);
    }

    [Fact]
    public async Task Prune_OptionsOverrideAttribute_AndZeroClearsTheLimit()
    {
        using var context = CreateContext();
        var invoice = await SaveInvoiceVersionsAsync(context, 5);

        var cleared = await Pruner(new SnapshotRetentionOptions { Retention = { ["Invoice"] = new SnapshotRetention { KeepLast = 0 } } }).PruneAsync(context);
        cleared.Should().NotContainKey("Invoice");
        (await VersionsAsync(context, invoice.Id)).Should().Equal(0, 1, 2, 3, 4);

        var tightened = await Pruner(new SnapshotRetentionOptions { Retention = { ["Invoice"] = new SnapshotRetention { KeepLast = 1 } } }).PruneAsync(context);
        tightened["Invoice"].Should().Be(4);
        (await VersionsAsync(context, invoice.Id)).Should().Equal(4);
    }

    [Fact]
    public async Task Prune_MaxAgeOnly_RemovesOldSnapshotsButNeverTheNewest()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Old");
        context.Add(ticket);
        await context.SaveChangesAsync();
        ticket.Rename("Old 2");
        await context.SaveChangesAsync();
        ticket.Rename("Old 3");
        await context.SaveChangesAsync();
        var options = new SnapshotRetentionOptions { Retention = { ["Ticket"] = new SnapshotRetention { MaxAgeDays = 30 } } };

        var young = await Pruner(options, daysAhead: 10).PruneAsync(context);
        young["Ticket"].Should().Be(0);
        (await VersionsAsync(context, ticket.Id)).Should().Equal(0, 1, 2);

        var aged = await Pruner(options, daysAhead: 60).PruneAsync(context);
        aged["Ticket"].Should().Be(2);
        (await VersionsAsync(context, ticket.Id)).Should().Equal(2);
    }

    [Fact]
    public async Task Prune_WithBothLimits_RemovesOnlySnapshotsBeyondBoth()
    {
        using var context = CreateContext();
        var invoice = await SaveInvoiceVersionsAsync(context, 5);
        var options = new SnapshotRetentionOptions { Retention = { ["Invoice"] = new SnapshotRetention { KeepLast = 2, MaxAgeDays = 30 } } };

        var young = await Pruner(options, daysAhead: 10).PruneAsync(context);
        young["Invoice"].Should().Be(0);
        (await VersionsAsync(context, invoice.Id)).Should().Equal(0, 1, 2, 3, 4);

        var aged = await Pruner(options, daysAhead: 60).PruneAsync(context);
        aged["Invoice"].Should().Be(3);
        (await VersionsAsync(context, invoice.Id)).Should().Equal(3, 4);
    }

    [Fact]
    public async Task Prune_RanksByNewerSnapshots_WhenVersionsHaveGaps()
    {
        using var context = CreateContext();
        var invoice = await SaveInvoiceVersionsAsync(context, 5);
        await context.Set<EntitySnapshot>().Where(s => s.EntityId == invoice.Id && (s.Version == 2 || s.Version == 3)).ExecuteDeleteAsync();

        var result = await Pruner().PruneAsync(context);

        result["Invoice"].Should().Be(1);
        (await VersionsAsync(context, invoice.Id)).Should().Equal(1, 4);
    }

    [Fact]
    public async Task Prune_AppliesLimitsPerEntity()
    {
        using var context = CreateContext();
        var first = await SaveInvoiceVersionsAsync(context, 4);
        var second = await SaveInvoiceVersionsAsync(context, 1);

        await Pruner().PruneAsync(context);

        (await VersionsAsync(context, first.Id)).Should().Equal(2, 3);
        (await VersionsAsync(context, second.Id)).Should().Equal(0);
    }

    [Fact]
    public async Task Prune_DryRun_CountsWithoutDeleting()
    {
        using var context = CreateContext();
        var invoice = await SaveInvoiceVersionsAsync(context, 5);

        var result = await Pruner().PruneAsync(context, dryRun: true);

        result["Invoice"].Should().Be(3);
        (await VersionsAsync(context, invoice.Id)).Should().Equal(0, 1, 2, 3, 4);
    }

    [Fact]
    public async Task Prune_DeletesInBatches()
    {
        using var context = CreateContext();
        var invoice = await SaveInvoiceVersionsAsync(context, 9);

        var result = await Pruner(new SnapshotRetentionOptions { BatchSize = 3 }).PruneAsync(context);

        result["Invoice"].Should().Be(7);
        (await VersionsAsync(context, invoice.Id)).Should().Equal(7, 8);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
