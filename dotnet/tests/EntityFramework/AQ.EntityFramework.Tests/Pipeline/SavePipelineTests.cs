using AQ.Actors;
using AQ.EntityFramework.Extensions;
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
        PeriodicReview = true,
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
        ticket.Verification.Add(Guid.NewGuid());
        await context.SaveChangesAsync();
        ticket.Verification.Count.Should().Be(1);

        ticket.SetNotes("not verifiable");
        await context.SaveChangesAsync();
        ticket.Verification.Count.Should().Be(1);

        ticket.Rename("verifiable change");
        await context.SaveChangesAsync();
        ticket.Verification.Count.Should().Be(0);
        ticket.Verification.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task VerificationState_PersistsCountAndEntries_AndTouchesOwnerAudit()
    {
        var verifier = Guid.NewGuid();
        Guid ticketId;
        using (var context = CreateContext())
        {
            var ticket = Ticket.Create("Persisted");
            context.Add(ticket);
            await context.SaveChangesAsync();
            ticketId = ticket.Id;
            ticket.Verification.Add(verifier);
            await context.SaveChangesAsync();
            ticket.Revision.Should().Be(1);
            ticket.UpdatedById.Should().Be(_userId);
        }

        using var reloaded = _database.CreateContext(All, _userId);
        var loaded = await reloaded.Tickets.SingleAsync(t => t.Id == ticketId);

        loaded.Verification.Count.Should().Be(1);
        loaded.Verification.HasBeenVerifiedBy(verifier).Should().BeTrue();
        loaded.Revision.Should().Be(1);
    }

    [Fact]
    public async Task VerificationReset_ResetsWhenOwnedVerifiedPropertyChanges()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Owned");
        ticket.SetPrice(new Price { Amount = 1, Currency = "USD" });
        context.Add(ticket);
        await context.SaveChangesAsync();
        ticket.Verification.Add(Guid.NewGuid());
        await context.SaveChangesAsync();

        ticket.SetNotes("owner is modified through a non-verifiable property");
        ticket.Price!.Amount = 2;
        await context.SaveChangesAsync();

        ticket.Verification.Count.Should().Be(0);
    }

    [Fact]
    public async Task ReviewState_Persists_AndTouchesOwnerAudit()
    {
        var reviewedAt = DateTimeOffset.UtcNow;
        var nextDue = DateTimeOffset.UtcNow.AddDays(30);
        Guid ticketId;
        using (var context = CreateContext())
        {
            var ticket = Ticket.Create("Reviewed");
            context.Add(ticket);
            await context.SaveChangesAsync();
            ticketId = ticket.Id;
            ticket.PeriodicReview.MarkReviewed(reviewedAt, nextDue);
            await context.SaveChangesAsync();
            ticket.Revision.Should().Be(1);
            ticket.UpdatedById.Should().Be(_userId);
        }

        using var reloaded = _database.CreateContext(All, _userId);
        var loaded = await reloaded.Tickets.SingleAsync(t => t.Id == ticketId);

        loaded.PeriodicReview.LastReviewedAt.Should().BeCloseTo(reviewedAt, TimeSpan.FromSeconds(1));
        loaded.PeriodicReview.NextReviewDue.Should().BeCloseTo(nextDue, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ReviewState_NeverReviewedRow_LoadsWithEmptyReviewNotNull()
    {
        Guid ticketId;
        using (var context = CreateContext())
        {
            var ticket = Ticket.Create("Untouched");
            context.Add(ticket);
            await context.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        using var reloaded = _database.CreateContext(All, _userId);
        var loaded = await reloaded.Tickets.SingleAsync(t => t.Id == ticketId);

        loaded.PeriodicReview.Should().NotBeNull();
        loaded.PeriodicReview.LastReviewedAt.Should().BeNull();
    }

    [Fact]
    public async Task Review_AndVerification_AreIndependent()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Independent");
        context.Add(ticket);
        await context.SaveChangesAsync();
        var verifier = Guid.NewGuid();
        ticket.Verification.Add(verifier);
        await context.SaveChangesAsync();

        ticket.PeriodicReview.MarkReviewed(DateTimeOffset.UtcNow, null);
        await context.SaveChangesAsync();
        ticket.Verification.Count.Should().Be(1);

        ticket.Rename("verifiable change");
        await context.SaveChangesAsync();

        ticket.Verification.Count.Should().Be(0);
        ticket.PeriodicReview.LastReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ReviewState_DerivedByTheApp_PersistsItsOwnColumnsBesideTheDates()
    {
        var reviewedAt = DateTimeOffset.UtcNow;
        Guid contractId;
        using (var context = CreateContext())
        {
            var contract = Contract.Create();
            context.Add(contract);
            await context.SaveChangesAsync();
            contractId = contract.Id;
            contract.PeriodicReview.Outcome = "Accepted";
            contract.PeriodicReview.MarkReviewed(reviewedAt, reviewedAt.AddDays(180));
            await context.SaveChangesAsync();
        }

        using var reloaded = _database.CreateContext(All, _userId);
        var loaded = await reloaded.Contracts.SingleAsync(c => c.Id == contractId);

        loaded.PeriodicReview.Outcome.Should().Be("Accepted");
        loaded.PeriodicReview.LastReviewedAt.Should().BeCloseTo(reviewedAt, TimeSpan.FromSeconds(1));
        loaded.PeriodicReview.NextReviewDue.Should().BeCloseTo(reviewedAt.AddDays(180), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task OwnedValueObjectOnlyChange_BumpsOwnerRevisionAndAudit()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Owned only");
        ticket.SetPrice(new Price { Amount = 1, Currency = "USD" });
        context.Add(ticket);
        await context.SaveChangesAsync();
        var revision = ticket.Revision;

        ticket.Price!.Amount = 2;
        await context.SaveChangesAsync();

        ticket.Revision.Should().Be(revision + 1);
    }

    [Fact]
    public async Task ClearingOwnedReference_BumpsOwnerRevision()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Cleared");
        ticket.SetPrice(new Price { Amount = 1, Currency = "USD" });
        context.Add(ticket);
        await context.SaveChangesAsync();
        var revision = ticket.Revision;

        ticket.SetPrice(null);
        await context.SaveChangesAsync();

        ticket.Revision.Should().Be(revision + 1);
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
    public async Task Lifecycle_CallsOnUpdateAndEvaluatesActiveFlagOnlyOnAddedAndModifiedEntities()
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
        untouched.EvaluationCount.Should().Be(evaluationsBefore);
        changed.EvaluationCount.Should().BeGreaterThan(0);
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
    public async Task Pipeline_WithoutActor_ThrowsMissingActorException()
    {
        using var context = _database.CreateContext(new AqSavePipelineOptions { Audit = true }, userId: null);
        context.Add(Ticket.Create("Anonymous"));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<MissingActorException>();
    }

    [Fact]
    public async Task Pipeline_WithoutActor_AllowsWritesThatAreNeitherAuditableNorEvented()
    {
        using var context = _database.CreateContext(All, userId: null);
        context.Users.Add(new TestUser { Id = Guid.NewGuid() });

        var saved = await context.SaveChangesAsync();

        saved.Should().Be(1);
    }

    [Fact]
    public async Task Pipeline_WithoutActor_ThrowsForEventsRaisedWithoutOtherChanges()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Quiet events");
        context.Add(ticket);
        await context.SaveChangesAsync();
        _database.Actors.Actor = null;
        ticket.Raise();

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<MissingActorException>();
    }

    [Fact]
    public async Task Audit_StartsUpdatedByEqualToCreatedBy()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Stamped");
        context.Add(ticket);

        await context.SaveChangesAsync();

        ticket.CreatedById.Should().Be(_userId);
        ticket.UpdatedById.Should().Be(_userId);
    }

    [Fact]
    public async Task Outbox_CarriesActorIncludingDelegation()
    {
        var onBehalfOf = Guid.NewGuid();
        using var context = CreateContext();
        var ticket = Ticket.Create("Delegated");
        context.Add(ticket);
        await context.SaveChangesAsync();
        _database.Actors.Actor = new Actor(_userId, PrincipalKind.Service, onBehalfOf, "client-1", "trace-1");
        ticket.Raise();

        await context.SaveChangesAsync();

        var stored = await context.OutboxEvents.SingleAsync();
        stored.ActorId.Should().Be(_userId);
        stored.ActorKind.Should().Be(PrincipalKind.Service);
        stored.OnBehalfOfId.Should().Be(onBehalfOf);
        stored.ClientId.Should().Be("client-1");
        stored.CorrelationId.Should().Be("trace-1");
        stored.ToActor().Should().Be(_database.Actors.Actor);
    }

    [Fact]
    public async Task Outbox_UsesConfiguredJsonOptions()
    {
        var options = new AqSavePipelineOptions
        {
            Outbox = true,
            Audit = true,
            OutboxJson = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase },
        };
        using var context = CreateContext(options);
        var ticket = Ticket.Create("Camel");
        context.Add(ticket);
        await context.SaveChangesAsync();
        ticket.Raise();

        await context.SaveChangesAsync();

        (await context.OutboxEvents.SingleAsync()).EventData.Should().Contain("\"actorId\"");
    }

    [Fact]
    public async Task ExecuteAuditedUpdate_StampsActorTimestampAndRevision()
    {
        using var context = CreateContext();
        var ticket = Ticket.Create("Bulk");
        context.Add(ticket);
        await context.SaveChangesAsync();
        var other = Guid.NewGuid();
        context.Users.Add(new TestUser { Id = other });
        await context.SaveChangesAsync();
        _database.Actors.Actor = new Actor(other, PrincipalKind.System);

        var updated = await context.Tickets.Where(t => t.Id == ticket.Id)
            .ExecuteAuditedUpdateAsync(_database.Actors, s => s.SetProperty(t => t.Notes, "bulk"));

        updated.Should().Be(1);
        var reloaded = await _database.CreateContext(All, _userId).Tickets.AsNoTracking().SingleAsync(t => t.Id == ticket.Id);
        reloaded.Notes.Should().Be("bulk");
        reloaded.UpdatedById.Should().Be(other);
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
