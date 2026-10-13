using AQ.Identity.Core.Auditing;
using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.KeyManagement;
using FastEndpoints;
using OpenIddict.Validation.AspNetCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Keys;

public class RotateKeysEndpoint(SigningKeyManager signingKeyManager, IIdentityDbContext context)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/manage/keys/rotate");
        Policies("ManageApi");
        // Machine API: bearer tokens (OpenIddict validation), never the browser cookie
        AuthSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        signingKeyManager.RotateNow();

        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.KeyRotated, AuditActors.Require(User)));
        await context.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
