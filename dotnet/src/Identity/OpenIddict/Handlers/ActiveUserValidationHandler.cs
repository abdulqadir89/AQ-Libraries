using AQ.Identity.Core.Entities;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Validation;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Handlers;

/// <summary>
/// Rejects access tokens of inactive users or whose SecurityStamp changed (forced re-auth after
/// security events). Registered as a scoped handler so dependencies come from the dispatch scope,
/// which is always available (unlike the transaction's "service_provider" property, which is
/// absent for some tokens, e.g. client_credentials, and made the previous inline handler throw).
/// </summary>
public class ActiveUserValidationHandler(UserManager<ApplicationUser> userManager)
    : IOpenIddictValidationHandler<OpenIddictValidationEvents.ValidateTokenContext>
{
    public async ValueTask HandleAsync(OpenIddictValidationEvents.ValidateTokenContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sub = context.Principal?.FindFirst(Claims.Subject)?.Value;
        if (sub == null || !Guid.TryParse(sub, out var userId)) return;

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is { IsActive: false })
        {
            context.Reject(Errors.InvalidToken, "Account is disabled.");
            return;
        }

        var tokenStamp = context.Principal?.FindFirst("stamp")?.Value;
        if (tokenStamp != null && user?.SecurityStamp != null && tokenStamp != user.SecurityStamp)
        {
            context.Reject(Errors.InvalidToken, "Token has been invalidated.");
        }
    }
}
