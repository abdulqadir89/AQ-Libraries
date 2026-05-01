using AQ.Identity.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AQ.Identity.OpenIddict.Health;

internal class IdentityMigrationHealthCheck<TContext>(TContext context) : IHealthCheck
    where TContext : DbContext, IIdentityDbContext
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext ctx, CancellationToken ct = default)
    {
        var pending = await context.Database.GetPendingMigrationsAsync(ct);
        return pending.Any()
            ? HealthCheckResult.Degraded($"Pending migrations: {string.Join(", ", pending)}")
            : HealthCheckResult.Healthy();
    }
}
