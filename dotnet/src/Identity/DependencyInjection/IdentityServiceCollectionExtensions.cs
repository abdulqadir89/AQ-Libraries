using AQ.Identity.OpenIddict.Extensions;
using AQ.Identity.OpenIddict.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AQ.Identity.DependencyInjection;

/// <summary>
/// Main extension methods for configuring AQ Identity.
///
/// The core AddAqIdentity{TContext} extension is in AQ.Identity.OpenIddict.Extensions.ServiceCollectionExtensions
/// Additional extensions:
/// - MapAqIdentityHealthChecks(IEndpointRouteBuilder) - maps /health and /health/ready endpoints
/// - AddAqIdentityLogging(IHostBuilder) - configures Serilog with JSON console sink
/// </summary>
public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddAqIdentity(this IServiceCollection services)
    {
        // ...existing code...

        services.AddHostedService<KeyRotationWorker>();

        return services;
    }
}