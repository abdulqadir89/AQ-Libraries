using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.KeyManagement;
using FastEndpoints;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Keys;

public class RotateKeysEndpoint(SigningKeyManager signingKeyManager, IIdentityDbContext context)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/manage/keys/rotate");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        signingKeyManager.RotateNow();

        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.KeyRotated, userId: null, ip: null, ua: null));
        await context.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
