using AQ.Identity.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AQ.Identity.OpenIddict.Health;

internal class IdentityDbHealthCheck<TContext>(TContext context) : IHealthCheck
    where TContext : DbContext, IIdentityDbContext
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext ctx, CancellationToken ct = default)
    {
        var canConnect = await context.Database.CanConnectAsync(ct);
        return canConnect
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Cannot connect to the identity database.");
    }
}
