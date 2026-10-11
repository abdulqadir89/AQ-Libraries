using AQ.Abstractions;
using AQ.Entities;
using AQ.Events.Domain;
using AQ.Utilities.References;
using AQ.Utilities.Snapshots;
using AQ.Utilities.Verification;

namespace AQ.EntityFramework.Tests.Support;

public sealed class TicketRaised(Guid aggregateId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; set; } = DateTimeOffset.UtcNow;
    public Guid AggregateId { get; set; } = aggregateId;
    public Guid? UserId { get; set; }
}

public class Price
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
}

[Snapshottable(IncludeId = true)]
public class Ticket : Entity<TestUser>, IVerifiable, IHasActiveFlag
{
    private Ticket() { }

    [Snapshot] [Verifiable] public string Title { get; private set; } = "";
    [Snapshot] public string Notes { get; private set; } = "";
    [Snapshot] [GeneratedReference(typeof(ShortCodeReferenceGenerator), 6, Prefix = "TCK")] public string Reference { get; private set; } = "";
    [Verifiable] public Price? Price { get; private set; }

    public bool IsActive { get; private set; } = true;
    public int EvaluationCount { get; private set; }
    public int AddCalls { get; private set; }
    public int UpdateCalls { get; private set; }
    public bool RaiseOnAdd { get; set; }

    public int VerificationCount { get; private set; }
    public List<VerificationEntry> CurrentVerifications { get; private set; } = [];
    IReadOnlyList<VerificationEntry> IVerifiable.CurrentVerifications => CurrentVerifications;

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

    public void ResetVerification()
    {
        VerificationCount = 0;
        CurrentVerifications = [];
    }

    public void AddVerification(Guid verifiedByUserId)
    {
        VerificationCount++;
        CurrentVerifications.Add(new VerificationEntry { VerifiedByUserId = verifiedByUserId, VerifiedAt = DateTimeOffset.UtcNow });
    }

    public bool HasBeenVerifiedBy(Guid userId) => CurrentVerifications.Any(v => v.VerifiedByUserId == userId);
}
