using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace AQ.Identity.OpenIddict.Extensions.Claims;

public class StoredClaimsPrincipalFactory<TContext>(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<IdentityOptions> optionsAccessor,
    TContext context)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>(userManager, roleManager, optionsAccessor)
    where TContext : IIdentityDbContext
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var storedClaims = await context.StoredClaims
            .AsNoTracking()
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        foreach (var claim in storedClaims)
            identity.AddClaim(new Claim(claim.Type, claim.Value));

        return identity;
    }
}
