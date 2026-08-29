using System.Linq;
using System.Security.Cryptography;
using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using FastEndpoints;
using OpenIddict.Abstractions;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Clients;

public class CreateClientResponse
{
    public string? ClientSecret { get; set; }
}

public class CreateClientEndpoint(
    IOpenIddictApplicationManager applicationManager,
    IIdentityDbContext context)
    : Endpoint<IdentityClientConfig, CreateClientResponse>
{
    public override void Configure()
    {
        Post("/manage/clients");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(IdentityClientConfig req, CancellationToken ct)
    {
        var redirectUriError = ClientDescriptorBuilder.ValidateRedirectUris(
            req.RedirectUris.Concat(req.PostLogoutRedirectUris));
        if (redirectUriError != null)
        {
            ThrowError(redirectUriError);
        }

        // Check for duplicate
        var existing = await applicationManager.FindByClientIdAsync(req.ClientId, ct);
        if (existing != null)
        {
            ThrowError($"Client '{req.ClientId}' already exists", statusCode: 409);
        }

        // Secrets for confidential clients are always generated server-side, never
        // accepted from the caller — prevents an admin from setting a weak/reused secret.
        string? generatedSecret = null;
        if (string.Equals(req.Type, OpenIddictConstants.ClientTypes.Confidential, StringComparison.OrdinalIgnoreCase))
        {
            generatedSecret = RandomNumberGenerator.GetHexString(64);
            req.ClientSecret = generatedSecret;
        }

        var descriptor = ClientDescriptorBuilder.Build(req);
        await applicationManager.CreateAsync(descriptor, ct);

        var auditEntry = AuditEntry.Log(
            AuditEntry.Actions.ClientCreated,
            userId: null,
            ip: null,
            ua: null);
        context.AuditLog.Add(auditEntry);
        await context.SaveChangesAsync(ct);

        await Send.CreatedAtAsync<GetAllClientsEndpoint>(
            null,
            new CreateClientResponse { ClientSecret = generatedSecret },
            cancellation: ct);
    }
}
