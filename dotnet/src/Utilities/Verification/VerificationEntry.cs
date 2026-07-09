namespace AQ.Abstractions;

public class VerificationEntry
{
    public Guid VerifiedByUserId { get; set; }
    public DateTimeOffset VerifiedAt { get; set; }
}
