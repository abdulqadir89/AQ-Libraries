using AQ.Common.Application.Services;
using AQ.Common.Application.Specifications.Builders;
using System.Linq.Expressions;

namespace AQ.Common.Application.Extensions;

/// <summary>
/// Extension methods for IApplicationDbContext to provide fluent query capabilities
/// </summary>
public static class ApplicationDbContextExtensions
{
    /// <summary>
    /// Start building a query specification for entity type T
    /// </summary>
    public static SpecificationBuilder<T> Query<T>(this IApplicationDbContext context) where T : class
    {
        return SpecificationBuilder<T>.Create();
    }

    /// <summary>
    /// Get entities using a fluent specification builder
    /// </summary>
    public static async Task<List<T>> GetListAsync<T>(
        this IApplicationDbContext context,
        Func<SpecificationBuilder<T>, SpecificationBuilder<T>> specBuilder,
        CancellationToken cancellationToken = default) where T : class
    {
        var specification = specBuilder(SpecificationBuilder<T>.Create()).Build();
        return await context.GetListAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Get a single entity using a fluent specification builder
    /// </summary>
    public static async Task<T?> GetAsync<T>(
        this IApplicationDbContext context,
        Func<SpecificationBuilder<T>, SpecificationBuilder<T>> specBuilder,
        CancellationToken cancellationToken = default) where T : class
    {
        var specification = specBuilder(SpecificationBuilder<T>.Create()).Build();
        return await context.GetAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Count entities using a fluent specification builder
    /// </summary>
    public static async Task<int> CountAsync<T>(
        this IApplicationDbContext context,
        Func<SpecificationBuilder<T>, SpecificationBuilder<T>> specBuilder,
        CancellationToken cancellationToken = default) where T : class
    {
        var specification = specBuilder(SpecificationBuilder<T>.Create()).Build();
        return await context.CountAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Check if any entities exist using a fluent specification builder
    /// </summary>
    public static async Task<bool> AnyAsync<T>(
        this IApplicationDbContext context,
        Func<SpecificationBuilder<T>, SpecificationBuilder<T>> specBuilder,
        CancellationToken cancellationToken = default) where T : class
    {
        var specification = specBuilder(SpecificationBuilder<T>.Create()).Build();
        return await context.AnyAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Get entity by ID with includes using a fluent builder
    /// </summary>
    public static async Task<T?> GetByIdWithIncludesAsync<T>(
        this IApplicationDbContext context,
        object id,
        Func<SpecificationBuilder<T>, SpecificationBuilder<T>> includeBuilder,
        CancellationToken cancellationToken = default) where T : class
    {
        var includeSpec = includeBuilder(SpecificationBuilder<T>.Create()).BuildIncludeSpec();
        return await context.GetByIdAsync(id, includeSpec, cancellationToken);
    }

    /// <summary>
    /// Simple include using string notation
    /// </summary>
    public static async Task<T?> GetByIdAsync<T>(
        this IApplicationDbContext context,
        object id,
        string includeProperty,
        CancellationToken cancellationToken = default) where T : class
    {
        var includeSpec = SpecificationBuilder<T>.Create()
            .Include(includeProperty)
            .BuildIncludeSpec();

        return await context.GetByIdAsync(id, includeSpec, cancellationToken);
    }

    /// <summary>
    /// Simple include using expression
    /// </summary>
    public static async Task<T?> GetByIdAsync<T, TProperty>(
        this IApplicationDbContext context,
        object id,
        Expression<Func<T, TProperty>> includeExpression,
        CancellationToken cancellationToken = default) where T : class
    {
        var includeSpec = SpecificationBuilder<T>.Create()
            .Include(includeExpression)
            .BuildIncludeSpec();

        return await context.GetByIdAsync(id, includeSpec, cancellationToken);
    }
}
