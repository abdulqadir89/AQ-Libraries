using AQ.Utilities.Verification;

namespace AQ.Abstractions;

public interface IVerifiable
{
    VerificationState Verification { get; }
}
