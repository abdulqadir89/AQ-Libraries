using Microsoft.Extensions.Options;

namespace AQ.Identity.Core.Configuration.Validation;

/// <summary>NIST SP 800-63B sec 5.1.1.2: passwords must be at least 8 characters.</summary>
public static class PasswordPolicyOptionsValidator
{
    public static ValidateOptionsResult Validate(PasswordPolicyOptions options)
    {
        if (options.MinLength < 8)
        {
            return ValidateOptionsResult.Fail(
                $"Password.MinLength must be at least 8 (NIST SP 800-63B minimum); got {options.MinLength}.");
        }

        return ValidateOptionsResult.Success;
    }
}
