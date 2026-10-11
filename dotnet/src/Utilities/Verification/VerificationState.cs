using AQ.Entities;

namespace AQ.Utilities.Verification;

public sealed class VerificationState
{
    public int Count { get; private set; }
    public List<VerificationEntry> Entries { get; private set; } = [];

    public void Reset()
    {
        Count = 0;
        Entries = [];
    }

    public void Add(Guid verifiedByUserId)
    {
        Count++;
        Entries.Add(new VerificationEntry { VerifiedByUserId = verifiedByUserId, VerifiedAt = DateTimeOffset.UtcNow });
    }

    public bool HasBeenVerifiedBy(Guid userId) => Entries.Any(v => v.VerifiedByUserId == userId);
}
