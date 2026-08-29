using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AQ.Identity.Core.Configuration.Validation;

/// <summary>
/// Aggregates the per-section validators so a single <c>IValidateOptions&lt;AqIdentityOptions&gt;</c>
/// registered with <c>ValidateOnStart()</c> fails fast (at <c>IHost.StartAsync</c>) on any
/// nonsensical security-relevant config value, instead of accepting it silently.
/// </summary>
public class AqIdentityOptionsValidator(ILogger<TokenLifetimeOptionsValidator> tokenLogger)
    : IValidateOptions<AqIdentityOptions>
{
    public ValidateOptionsResult Validate(string? name, AqIdentityOptions options)
    {
        var failures = new List<string>();

        var passwordResult = PasswordPolicyOptionsValidator.Validate(options.Password);
        if (passwordResult.Failed) failures.AddRange(passwordResult.Failures!);

        var lockoutResult = LockoutPolicyOptionsValidator.Validate(options.Lockout);
        if (lockoutResult.Failed) failures.AddRange(lockoutResult.Failures!);

        var tokenResult = new TokenLifetimeOptionsValidator(tokenLogger).Validate(options.Tokens);
        if (tokenResult.Failed) failures.AddRange(tokenResult.Failures!);

        var keyResult = KeyManagementOptionsValidator.Validate(options.Keys);
        if (keyResult.Failed) failures.AddRange(keyResult.Failures!);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
