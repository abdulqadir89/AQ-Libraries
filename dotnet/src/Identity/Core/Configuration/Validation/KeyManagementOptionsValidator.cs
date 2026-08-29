using Microsoft.Extensions.Options;

namespace AQ.Identity.Core.Configuration.Validation;

public static class KeyManagementOptionsValidator
{
    public static ValidateOptionsResult Validate(KeyManagementOptions options)
    {
        if (options.RotationPeriod <= TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail(
                $"Keys.RotationPeriod must be greater than zero; got {options.RotationPeriod}.");
        }

        if (options.RetirementOverlap < TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail(
                $"Keys.RetirementOverlap cannot be negative; got {options.RetirementOverlap}.");
        }

        if (options.RetirementOverlap >= options.RotationPeriod)
        {
            return ValidateOptionsResult.Fail(
                $"Keys.RetirementOverlap ({options.RetirementOverlap}) must be less than " +
                $"Keys.RotationPeriod ({options.RotationPeriod}) — otherwise keys never truly retire.");
        }

        return ValidateOptionsResult.Success;
    }
}
