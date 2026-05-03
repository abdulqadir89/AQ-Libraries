using AQ.Identity.Core.Entities;
using AQ.Identity.Core.Abstractions;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Users;

public class InvalidateUserTokensRequest
{
    public Guid UserId { get; set; }
}

public class InvalidateUserTokensEndpoint(
    IIdentityDbContext context,
    UserManager<ApplicationUser> userManager)
    : Endpoint<InvalidateUserTokensRequest>
{
    public override void Configure()
    {
        Put("/manage/users/{UserId}/invalidate-tokens");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(InvalidateUserTokensRequest req, CancellationToken ct)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == req.UserId, ct);

        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Rotating SecurityStamp causes all existing access tokens to fail
        // validation on the next request (ValidateTokenContext stamp check)
        await userManager.UpdateSecurityStampAsync(user);

        context.AuditLog.Add(AuditEntry.Log(
            AuditEntry.Actions.UserTokensInvalidated,
            user.Id,
            null,
            null));
        await context.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
