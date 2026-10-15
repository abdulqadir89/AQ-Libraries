using AQ.Entities;

namespace AQ.Utilities.Verification;

public sealed class VerificationState
{
    public int Count { get; private set; }
    public List<VerificationEntry> Entries { get; private set; } = [];
    public DateTimeOffset? LastResetAt { get; private set; }

    // RULE VER-1
    public void Reset()
    {
        Count = 0;
        Entries = [];
        LastResetAt = DateTimeOffset.UtcNow;
    }

    public void Add(Guid verifiedByUserId)
    {
        Count++;
        Entries.Add(new VerificationEntry { VerifiedByUserId = verifiedByUserId, VerifiedAt = DateTimeOffset.UtcNow });
    }

    // RULE VER-2
    public bool HasBeenVerifiedBy(Guid userId) => Entries.Any(v => v.VerifiedByUserId == userId);
}
