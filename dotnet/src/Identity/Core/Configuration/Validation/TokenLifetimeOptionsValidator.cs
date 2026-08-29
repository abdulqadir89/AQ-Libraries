using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AQ.Identity.Core.Configuration.Validation;

/// <summary>OAuth 2.0 Security BCP sec 4.11: access tokens should be short-lived.</summary>
public class TokenLifetimeOptionsValidator(ILogger<TokenLifetimeOptionsValidator> logger)
{
    private static readonly TimeSpan LongAccessTokenWarningThreshold = TimeSpan.FromHours(24);

    public ValidateOptionsResult Validate(TokenLifetimeOptions options)
    {
        if (options.AccessToken <= TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail(
                $"Tokens.AccessToken must be greater than zero; got {options.AccessToken}.");
        }

        if (options.RefreshToken <= options.AccessToken)
        {
            return ValidateOptionsResult.Fail(
                $"Tokens.RefreshToken ({options.RefreshToken}) must be greater than Tokens.AccessToken ({options.AccessToken}).");
        }

        if (options.AccessToken > LongAccessTokenWarningThreshold)
        {
            logger.LogWarning(
                "Tokens.AccessToken is {AccessToken}, longer than the recommended short-lived " +
                "window (OAuth 2.0 Security BCP sec 4.11). Not rejected, but consider shortening it.",
                options.AccessToken);
        }

        return ValidateOptionsResult.Success;
    }
}
