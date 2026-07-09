namespace AQ.Abstractions;

public interface IVerificationHistoryEntry
{
    Guid Id { get; }
    Guid VerifiedByUserId { get; }
    DateTimeOffset VerifiedAt { get; }
}
