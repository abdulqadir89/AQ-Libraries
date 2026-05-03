using AQ.Identity.Core.Abstractions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Users;

public class DeleteUserClaimTypeRequest
{
    public Guid UserId { get; set; }
    public string ClaimType { get; set; } = string.Empty;
}

public class DeleteUserClaimTypeEndpoint(IIdentityDbContext context) : Endpoint<DeleteUserClaimTypeRequest>
{
    public override void Configure()
    {
        Delete("/manage/users/{UserId}/claims/{ClaimType}");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(DeleteUserClaimTypeRequest req, CancellationToken ct)
    {
        var claims = await context.StoredClaims
            .Where(c => c.UserId == req.UserId && c.Type == req.ClaimType)
            .ToListAsync(ct);

        if (claims.Count == 0)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        context.StoredClaims.RemoveRange(claims);
        await context.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
