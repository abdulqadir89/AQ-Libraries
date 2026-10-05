using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AQ.Identity.OpenIddict.Sessions;
using OpenIddict.Validation.AspNetCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Users;

public class DeleteUserSessionsRequest
{
    public Guid Id { get; set; }
}

public class DeleteUserSessionsEndpoint(
    IIdentityDbContext context,
    SessionRevocationService sessionRevocation,
    UserManager<ApplicationUser> userManager)
    : Endpoint<DeleteUserSessionsRequest>
{
    public override void Configure()
    {
        Delete("/manage/users/{Id}/sessions");
        Policies("ManageApi");
        // Machine API: bearer tokens (OpenIddict validation), never the browser cookie
        AuthSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(DeleteUserSessionsRequest req, CancellationToken ct)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == req.Id, ct);

        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // End every app session and token, and notify clients over back-channel logout
        await sessionRevocation.RevokeAllAsync(user.Id.ToString(), ct);

        // Rotate SecurityStamp so any in-flight access tokens are also rejected
        await userManager.UpdateSecurityStampAsync(user);

        context.AuditLog.Add(AuditEntry.Log(
            AuditEntry.Actions.UserSessionsRevoked,
            user.Id,
            null,
            null));
        await context.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
