using AQ.Identity.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.OpenIddict.Extensions.Seeding;

public interface ISetupStateService
{
    Task<bool> IsSetupRequiredAsync(CancellationToken ct = default);
}

public class SetupStateService(IIdentityDbContext context) : ISetupStateService
{
    public async Task<bool> IsSetupRequiredAsync(CancellationToken ct = default)
    {
        var hasAdmin = await context.StoredClaims
            .AsNoTracking()
            .AnyAsync(c => c.Type == "manage_api", ct);

        return !hasAdmin;
    }
}
