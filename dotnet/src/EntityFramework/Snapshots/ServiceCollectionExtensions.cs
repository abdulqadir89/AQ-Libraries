using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AQ.EntityFramework.Snapshots;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAqSnapshotPruning(this IServiceCollection services, Action<SnapshotRetentionOptions>? configure = null)
    {
        var options = new SnapshotRetentionOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<SnapshotPruner>();
        return services;
    }
}
