using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.Management.Dto;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Users;

public class UpsertUserClaimsEndpoint(IIdentityDbContext context) : Endpoint<UpsertUserClaimsRequest, List<UserClaimDto>>
{
    public override void Configure()
    {
        Put("/manage/users/{UserId}/claims");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(UpsertUserClaimsRequest req, CancellationToken ct)
    {
        var userExists = await context.Users.AnyAsync(u => u.Id == req.UserId, ct);
        if (!userExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Collect all claim types in this request and replace them atomically
        var incomingTypes = req.Claims
            .Select(c => c.Type)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = await context.StoredClaims
            .Where(c => c.UserId == req.UserId && incomingTypes.Contains(c.Type))
            .ToListAsync(ct);
        context.StoredClaims.RemoveRange(existing);

        var newClaims = req.Claims
            .Select(c => UserClaim.Create(req.UserId, c.Type, c.Value))
            .ToList();
        context.StoredClaims.AddRange(newClaims);

        await context.SaveChangesAsync(ct);

        var response = newClaims
            .Select(c => new UserClaimDto { Id = c.Id, Type = c.Type, Value = c.Value })
            .ToList();

        await Send.OkAsync(response, ct);
    }
}
