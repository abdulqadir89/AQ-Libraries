using Microsoft.Extensions.Hosting;
using Serilog;

namespace AQ.Identity.OpenIddict.Extensions;

public static class LoggingExtensions
{
    /// <summary>
    /// Configures Serilog with a JSON console sink and enrichers.
    /// Call on IHostBuilder before Build().
    /// </summary>
    public static IHostBuilder AddAqIdentityLogging(this IHostBuilder builder)
    {
        return builder.UseSerilog((context, config) =>
        {
            config
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("ApplicationName", context.HostingEnvironment.ApplicationName)
                .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
        });
    }
}
