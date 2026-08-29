using Microsoft.Extensions.Options;

namespace AQ.Identity.Core.Configuration.Validation;

public static class LockoutPolicyOptionsValidator
{
    public static ValidateOptionsResult Validate(LockoutPolicyOptions options)
    {
        if (options.MaxFailedAttempts < 1)
        {
            return ValidateOptionsResult.Fail(
                $"Lockout.MaxFailedAttempts must be at least 1; got {options.MaxFailedAttempts}.");
        }

        if (options.LockoutDuration <= TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail(
                $"Lockout.LockoutDuration must be greater than zero; got {options.LockoutDuration}.");
        }

        return ValidateOptionsResult.Success;
    }
}
