using AQ.Identity.Core.Abstractions;
using AQ.Identity.OpenIddict.Management.Dto;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Users;

public class GetUserClaimsRequest
{
    public Guid UserId { get; set; }
}

public class GetUserClaimsEndpoint(IIdentityDbContext context) : Endpoint<GetUserClaimsRequest, List<UserClaimDto>>
{
    public override void Configure()
    {
        Get("/manage/users/{UserId}/claims");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(GetUserClaimsRequest req, CancellationToken ct)
    {
        var userExists = await context.Users.AnyAsync(u => u.Id == req.UserId, ct);
        if (!userExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var claims = await context.StoredClaims
            .AsNoTracking()
            .Where(c => c.UserId == req.UserId)
            .Select(c => new UserClaimDto { Id = c.Id, Type = c.Type, Value = c.Value })
            .OrderBy(c => c.Type)
            .ToListAsync(ct);

        await Send.OkAsync(claims, ct);
    }
}
