using AQ.Abstractions;

namespace AQ.Extensions;

public static class MergeableExtensions
{
    /// <summary>
    /// Get all entities including merged ones that refer to this primary entity.
    /// </summary>
    public static IQueryable<T> WithMerged<T>(this IQueryable<T> query, Guid primaryId)
        where T : IEntity, IMergeable<T>
    {
        return query.Where(e => e.Id == primaryId || e.PrimaryId == primaryId);
    }

    /// <summary>
    /// Get the effective primary entity (self if primary, otherwise the one it's merged into).
    /// </summary>
    public static T? GetEffectivePrimary<T>(this T entity, IQueryable<T> queryable)
        where T : IEntity, IMergeable<T>
    {
        if (entity.IsPrimary) return entity;

        return queryable.FirstOrDefault(e => e.Id == entity.PrimaryId);
    }

    /// <summary>
    /// Get all records grouped by primary.
    /// </summary>
    public static IQueryable<IGrouping<Guid, T>> GroupByPrimary<T>(this IQueryable<T> query)
        where T : IEntity, IMergeable<T>
    {
        return query.GroupBy(e => e.PrimaryId ?? e.Id);
    }
}
