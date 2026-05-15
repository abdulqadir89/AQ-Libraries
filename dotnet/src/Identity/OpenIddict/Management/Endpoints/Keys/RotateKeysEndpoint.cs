using AQ.Identity.OpenIddict.KeyManagement;
using FastEndpoints;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Keys;

public class RotateKeysEndpoint(SigningKeyManager signingKeyManager)
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
        await Send.NoContentAsync(ct);
    }
}
