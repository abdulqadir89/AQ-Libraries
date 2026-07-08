using Microsoft.Extensions.DependencyInjection;

namespace AQ.Utilities.References;

public static class ReferenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers the built-in, non-database reference generation strategies for dependency injection.
    /// Sequential/counter-based generators are DB-dependent and must be registered by the consuming project.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The updated service collection</returns>
    public static IServiceCollection AddReferenceGeneration(this IServiceCollection services)
    {
        services.AddSingleton<ShortCodeReferenceGenerator>();
        services.AddSingleton<WordSlugReferenceGenerator>();
        return services;
    }
}
