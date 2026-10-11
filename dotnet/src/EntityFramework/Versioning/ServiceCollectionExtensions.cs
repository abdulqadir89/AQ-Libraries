using Microsoft.Extensions.DependencyInjection;

namespace AQ.EntityFramework.Versioning;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAqEntityVersioning(this IServiceCollection services)
    {
        services.AddScoped<EntityVersioningService>();
        return services;
    }
}
