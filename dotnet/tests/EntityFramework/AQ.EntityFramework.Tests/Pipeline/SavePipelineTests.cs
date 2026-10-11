using AQ.EntityFramework.SaveChanges;
using AQ.EntityFramework.Tests.Support;
using AQ.Events.Outbox;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Tests.Pipeline;

public class SavePipelineTests : IDisposable
{
    private static readonly AqSavePipelineOptions All = new()
    {
        Lifecycle = true,
        GeneratedReferences = true,
        VerificationReset = true,
        Audit = true,
        Snapshots = true,
        Outbox = true,
    };

    private readonly PipelineTestDatabase _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    private PipelineTestContext CreateContext(AqSavePipelineOptions? options = null)
    {
        var context = _database.CreateContext(options ?? All, _userId);
        context.Users.Add(new TestUser { Id = _userId });
        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task Audit_SetsCreatedFieldsOnAdd_AndUpdatedFieldsAndRevisionOnUpdate()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("First");
        context.Add(ticket);
        await context.SaveChangesAsync();

        ticket.CreatedById.Should().Be(_userId);
        ticket.Revision.Should().Be(0);
        ticket.UpdatedAt.Should().BeNull();

        ticket.SetNotes("changed");
        await context.SaveChangesAsync();

        ticket.UpdatedById.Should().Be(_userId);
        ticket.UpdatedAt.Should().NotBeNull();
        ticket.Revision.Should().Be(1);
    }

    [Fact]
    public async Task GeneratedReferences_FillsBlankReferenceOnAddButKeepsProvidedValue()
    {
        using var context = CreateContext();
        var generated = Ticket.Create("Generated");
        var provided = Ticket.Create("Provided", reference: "MANUAL-1");
        context.AddRange(generated, provided);

        await context.SaveChangesAsync();

        generated.Reference.Should().StartWith("TCK-").And.HaveLength(10);
        provided.Reference.Should().Be("MANUAL-1");
    }

    [Fact]
    public async Task VerificationReset_ResetsWhenVerifiedScalarChanges_ButNotWhenOtherPropertyChanges()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Verified");
        context.Add(ticket);
        await context.SaveChangesAsync();
        ticket.AddVerification(Guid.NewGuid());
        await context.SaveChangesAsync();
        ticket.VerificationCount.Should().Be(1);

        ticket.SetNotes("not verifiable");
        await context.SaveChangesAsync();
        ticket.VerificationCount.Should().Be(1);

        ticket.Rename("verifiable change");
        await context.SaveChangesAsync();
        ticket.VerificationCount.Should().Be(0);
        ticket.CurrentVerifications.Should().BeEmpty();
    }

    [Fact]
    public async Task VerificationReset_ResetsWhenOwnedVerifiedPropertyChanges()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Owned");
        ticket.SetPrice(new Price { Amount = 1, Currency = "USD" });
        context.Add(ticket);
        await context.SaveChangesAsync();
        ticket.AddVerification(Guid.NewGuid());
        await context.SaveChangesAsync();

        ticket.Price!.Amount = 2;
        await context.SaveChangesAsync();

        ticket.VerificationCount.Should().Be(0);
    }

    [Fact]
    public async Task Snapshots_CreatedAfterAuditStepWithVersionEqualToRevision()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Snap");
        context.Add(ticket);
        await context.SaveChangesAsync();
        ticket.Rename("Snap 2");
        await context.SaveChangesAsync();
        ticket.Rename("Snap 3");
        await context.SaveChangesAsync();

        var snapshots = await context.TicketSnapshots.Where(s => s.EntityId == ticket.Id).OrderBy(s => s.Version).ToListAsync();

        snapshots.Select(s => s.Version).Should().Equal(0, 1, 2);
        snapshots[2].Version.Should().Be(ticket.Revision);
        snapshots[0].Data.Should().Contain("\"title\":\"Snap\"");
        snapshots[2].Data.Should().Contain("\"title\":\"Snap 3\"");
    }

    [Fact]
    public async Task Snapshots_IncludeGeneratedReferenceBecauseReferencesRunFirst()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Ordered");
        context.Add(ticket);
        await context.SaveChangesAsync();

        var snapshot = await context.TicketSnapshots.SingleAsync(s => s.EntityId == ticket.Id);

        snapshot.Data.Should().Contain($"\"reference\":\"{ticket.Reference}\"");
    }

    [Fact]
    public async Task Outbox_StoresDomainEventsAndClearsThemFromEntity()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Evented");
        context.Add(ticket);
        await context.SaveChangesAsync();
        ticket.Raise();
        ticket.SetNotes("raise");

        await context.SaveChangesAsync();

        var stored = await context.OutboxEvents.ToListAsync();
        stored.Should().ContainSingle();
        stored[0].Category.Should().Be(OutboxEventCategory.Domain);
        stored[0].EventType.Should().Contain(nameof(TicketRaised));
        stored[0].EventData.Should().Contain(_userId.ToString());
        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Lifecycle_RunsBeforeOutbox_SoEventsRaisedInOnAddAreStored()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Lifecycle");
        ticket.RaiseOnAdd = true;
        context.Add(ticket);

        await context.SaveChangesAsync();

        ticket.AddCalls.Should().Be(1);
        (await context.OutboxEvents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Lifecycle_CallsOnUpdateAndEvaluatesActiveFlagOnAllTrackedEntities()
    {
        using var context = CreateContext();
        var changed = Ticket.Create("Changed");
        var untouched = Ticket.Create("Untouched");
        context.AddRange(changed, untouched);
        await context.SaveChangesAsync();
        var evaluationsBefore = untouched.EvaluationCount;

        changed.SetNotes("x");
        await context.SaveChangesAsync();

        changed.UpdateCalls.Should().Be(1);
        untouched.UpdateCalls.Should().Be(0);
        untouched.EvaluationCount.Should().Be(evaluationsBefore + 1);
    }

    [Fact]
    public async Task DisabledSteps_DoNotRun()
    {
        using var context = CreateContext(new AqSavePipelineOptions { Audit = true });
        var ticket = Ticket.Create("Audit only");
        ticket.RaiseOnAdd = true;
        context.Add(ticket);
        ticket.Raise();

        await context.SaveChangesAsync();

        ticket.CreatedById.Should().Be(_userId);
        ticket.Reference.Should().BeEmpty();
        ticket.AddCalls.Should().Be(0);
        (await context.TicketSnapshots.CountAsync()).Should().Be(0);
        (await context.OutboxEvents.CountAsync()).Should().Be(0);
        ticket.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task Pipeline_WithoutCurrentUserProvider_UsesNullUser()
    {
        using var context = _database.CreateContext(new AqSavePipelineOptions { Audit = true }, userId: null);
        var ticket = Ticket.Create("Anonymous");
        context.Add(ticket);

        await context.SaveChangesAsync();

        ticket.CreatedById.Should().BeNull();
    }

    [Fact]
    public async Task Outbox_StoresEventsRaisedOnEntityWithoutOtherChanges()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Quiet");
        context.Add(ticket);
        await context.SaveChangesAsync();
        ticket.Raise();

        await context.SaveChangesAsync();

        (await context.OutboxEvents.CountAsync()).Should().Be(1);
    }
}
