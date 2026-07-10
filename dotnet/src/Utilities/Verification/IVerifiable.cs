namespace AQ.Abstractions;

public interface IVerifiable
{
    int VerificationCount { get; }
    IReadOnlyList<VerificationEntry> CurrentVerifications { get; }

    void ResetVerification();
    void AddVerification(Guid verifiedByUserId);
    bool HasBeenVerifiedBy(Guid userId);
}
