using AQ.Entities;
using AQ.Events.Domain;
using AQ.Utilities.Commenting;
using AQ.Utilities.References;
using AQ.Utilities.Review;
using AQ.Utilities.Snapshots;
using AQ.Utilities.Tagging;
using AQ.Utilities.Verification;
using AQ.Utilities.Versioning;

namespace AQ.EntityFramework.Tests.Support;

public sealed class TicketRaised(Guid aggregateId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; set; } = DateTimeOffset.UtcNow;
    public Guid AggregateId { get; set; } = aggregateId;
    public Guid? ActorId { get; set; }
    public Guid? OnBehalfOfId { get; set; }
    public string? CorrelationId { get; set; }
}

public class Price
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
}

[Snapshottable(KeepLast = 2)]
public class Invoice : Entity<TestUser>
{
    private Invoice() { }

    [Snapshot] public string Number { get; private set; } = "";

    public static Invoice Create(string number) => new() { Number = number };

    public void Rename(string number) => Number = number;
}

[Audited]
[Snapshottable(IncludeId = true)]
public class Ticket : Entity<TestUser>, IVerifiable, IReviewable<ReviewState>, IHasActiveFlag
{
    private Ticket() { }

    [Snapshot][Verifiable] public string Title { get; private set; } = "";
    [Snapshot][AuditIgnore] public string Notes { get; private set; } = "";
    [Snapshot][GeneratedReference(typeof(ShortCodeReferenceGenerator), 6, Prefix = "TCK")] public string Reference { get; private set; } = "";
    [Verifiable] public Price? Price { get; private set; }

    public bool IsActive { get; private set; } = true;
    public int EvaluationCount { get; private set; }
    public int AddCalls { get; private set; }
    public int UpdateCalls { get; private set; }
    public bool RaiseOnAdd { get; set; }

    public VerificationState Verification { get; private set; } = new();
    public ReviewState Review { get; private set; } = new();

    public static Ticket Create(string title, string notes = "", string reference = "") =>
        new() { Title = title, Notes = notes, Reference = reference };

    public void Rename(string title) => Title = title;

    public void SetNotes(string notes) => Notes = notes;

    public void SetPrice(Price? price) => Price = price;

    public void Raise() => AddDomainEvent(new TicketRaised(Id));

    public void EvaluateIsActive() => EvaluationCount++;

    public override void OnAdd()
    {
        AddCalls++;
        if (RaiseOnAdd)
        {
            Raise();
        }
    }

    public override void OnUpdate() => UpdateCalls++;
}

[VersionGroup]
[Taggable(typeof(TestTag))]
[Snapshottable]
public partial class Manual : Entity<TestUser>, IVerifiable
{
    private Manual() { }

    [Snapshot] public string Title { get; private set; } = "";

    public VerificationState Verification { get; private set; } = new();

    public static Manual Create(string title) => new() { Title = title };

    public void Rename(string title) => Title = title;
}

public sealed class ContractReviewState : ReviewStateBase
{
    public string? Outcome { get; set; }
}

public class Contract : Entity<TestUser>, IReviewable<ContractReviewState>
{
    private Contract() { }

    public ContractReviewState Review { get; private set; } = new();

    public static Contract Create() => new();
}

public abstract class TestComment : Comment<TestUser>
{
    public string? Area { get; set; }
}

[Commentable(typeof(TestComment))]
public class Memo : Entity<TestUser>
{
    private Memo() { }

    public string Title { get; private set; } = "";

    public static Memo Create(string title) => new() { Title = title };
}
