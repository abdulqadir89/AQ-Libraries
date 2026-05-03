using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Users;

public class DeleteUserSessionsRequest
{
    public Guid Id { get; set; }
}

public class DeleteUserSessionsEndpoint(
    IIdentityDbContext context,
    IOpenIddictTokenManager tokenManager,
    UserManager<ApplicationUser> userManager)
    : Endpoint<DeleteUserSessionsRequest>
{
    public override void Configure()
    {
        Delete("/manage/users/{Id}/sessions");
        Policies("ManageApi");
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

        // Revoke all refresh tokens
        var tokens = tokenManager.FindBySubjectAsync(user.Id.ToString(), ct);
        await foreach (var token in tokens)
        {
            var type = await tokenManager.GetTypeAsync(token, ct);
            if (type != TokenTypeHints.RefreshToken) continue;

            var status = await tokenManager.GetStatusAsync(token, ct);
            if (status != Statuses.Revoked)
            {
                var tokenId = await tokenManager.GetIdAsync(token, ct);
                await tokenManager.RevokeAsync(tokenId, null, null, null, ct);
            }
        }

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
