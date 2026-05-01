using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using FastEndpoints;
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
    IOpenIddictTokenManager tokenManager)
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
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == req.Id, ct);

        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Revoke all refresh tokens for this user
        var tokens = tokenManager.FindBySubjectAsync(user.Id.ToString(), ct);
        await foreach (var token in tokens)
        {
            var type = await tokenManager.GetTypeAsync(token, ct);
            if (type != TokenTypeHints.RefreshToken)
            {
                continue;
            }

            var status = await tokenManager.GetStatusAsync(token, ct);
            if (status != Statuses.Revoked)
            {
                var tokenId = await tokenManager.GetIdAsync(token, ct);
                await tokenManager.RevokeAsync(tokenId, null, null, null, ct);
            }
        }

        var auditEntry = AuditEntry.Log(
            AuditEntry.Actions.UserSessionsRevoked,
            user.Id,
            null,
            null);
        context.AuditLog.Add(auditEntry);
        await context.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
