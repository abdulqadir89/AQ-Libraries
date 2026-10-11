using AQ.Utilities.Verification;

namespace AQ.Utilities.Verification;

public interface IVerifiable
{
    VerificationState Verification { get; }
}
