using AQ.Actors;
using AQ.EntityFramework.Auditing;
using AQ.EntityFramework.SaveChanges;
using AQ.EntityFramework.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AQ.EntityFramework.Tests.Pipeline;

public class AuditLogTests : IDisposable
{
    private static readonly AqSavePipelineOptions Options = new()
    {
        Audit = true,
        AuditLog = true,
        Lifecycle = true,
        Outbox = true,
        Snapshots = true,
        GeneratedReferences = true,
    };

    private readonly PipelineTestDatabase _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    private PipelineTestContext CreateContext()
    {
        var context = _database.CreateContext(Options, _userId);
        context.Users.Add(new TestUser { Id = _userId });
        context.SaveChanges();
        return context;
    }

    private static JsonElement Changes(AuditEvent e) => JsonDocument.Parse(e.ChangesJson).RootElement;

    [Fact]
    public async Task Insert_IsLogged_WithNewValuesAndActor()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("First", reference: "R-1");
        context.Add(ticket);

        await context.SaveChangesAsync();

        var audit = await context.AuditEvents.SingleAsync();
        audit.EntityType.Should().Be(nameof(Ticket));
        audit.EntityId.Should().Be(ticket.Id);
        audit.Operation.Should().Be(AuditOperation.Insert);
        audit.ActorId.Should().Be(_userId);
        audit.ActorKind.Should().Be(PrincipalKind.Human);
        Changes(audit).GetProperty("Title").GetProperty("new").GetString().Should().Be("First");
    }

    [Fact]
    public async Task Update_LogsOnlyChangedProperties_OldAndNew()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Before");
        context.Add(ticket);
        await context.SaveChangesAsync();

        ticket.Rename("After");
        await context.SaveChangesAsync();

        var update = await context.AuditEvents.SingleAsync(e => e.Operation == AuditOperation.Update);
        var changes = Changes(update);
        changes.GetProperty("Title").GetProperty("old").GetString().Should().Be("Before");
        changes.GetProperty("Title").GetProperty("new").GetString().Should().Be("After");
        changes.EnumerateObject().Select(p => p.Name).Should().NotContain(["Revision", "UpdatedAt", "UpdatedById"]);
    }

    [Fact]
    public async Task AuditIgnoreProperties_AreNeverRecorded()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Ignore", notes: "sensitive");
        context.Add(ticket);
        await context.SaveChangesAsync();
        ticket.SetNotes("also sensitive");
        ticket.Rename("Ignore 2");
        await context.SaveChangesAsync();

        var all = await context.AuditEvents.ToListAsync();

        all.Should().OnlyContain(e => !e.ChangesJson.Contains("Notes") && !e.ChangesJson.Contains("sensitive"));
    }

    [Fact]
    public async Task OwnedValueChange_IsFoldedIntoTheOwnersEvent()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Priced");
        ticket.SetPrice(new Price { Amount = 1, Currency = "USD" });
        context.Add(ticket);
        await context.SaveChangesAsync();

        ticket.Price!.Amount = 2;
        await context.SaveChangesAsync();

        var update = await context.AuditEvents.SingleAsync(e => e.Operation == AuditOperation.Update);
        var amount = Changes(update).GetProperty("Price.Amount");
        amount.GetProperty("old").GetDecimal().Should().Be(1);
        amount.GetProperty("new").GetDecimal().Should().Be(2);
    }

    [Fact]
    public async Task Delete_IsLogged_WithOldValues()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Doomed");
        context.Add(ticket);
        await context.SaveChangesAsync();

        context.Remove(ticket);
        await context.SaveChangesAsync();

        var delete = await context.AuditEvents.SingleAsync(e => e.Operation == AuditOperation.Delete);
        delete.EntityId.Should().Be(ticket.Id);
        Changes(delete).GetProperty("Title").GetProperty("old").GetString().Should().Be("Doomed");
    }

    [Fact]
    public async Task EntitiesWithoutAuditedAttribute_AreNotLogged()
    {
        using var context = CreateContext();
        context.Users.Add(new TestUser());

        await context.SaveChangesAsync();

        (await context.AuditEvents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AuditLog_IsAppendOnly()
    {
        using var context = CreateContext();
        context.Add(Ticket.Create("Append"));
        await context.SaveChangesAsync();
        var audit = await context.AuditEvents.SingleAsync();

        context.Remove(audit);
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*append-only*");
    }

    [Fact]
    public async Task AuditedChange_WithoutActor_Throws()
    {
        using var context = _database.CreateContext(new AqSavePipelineOptions { AuditLog = true }, userId: null);
        context.Add(Ticket.Create("Anonymous"));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<MissingActorException>();
    }

    [Fact]
    public async Task DisabledOption_WritesNoAuditEvents()
    {
        using var context = _database.CreateContext(new AqSavePipelineOptions { Audit = true }, _userId);
        context.Users.Add(new TestUser { Id = _userId });
        context.Add(Ticket.Create("Quiet"));

        await context.SaveChangesAsync();

        (await context.AuditEvents.CountAsync()).Should().Be(0);
    }
}
