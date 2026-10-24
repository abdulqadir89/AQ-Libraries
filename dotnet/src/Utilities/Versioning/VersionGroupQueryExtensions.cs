namespace AQ.Utilities.Versioning;

public static class VersionGroupQueryExtensions
{
    public static IQueryable<T> InVersionGroup<T>(this IQueryable<T> query, Guid versionGroupId)
        where T : IVersionGrouped
    {
        return query.Where(e => e.VersionGroupId == versionGroupId);
    }

    /// <summary>Records in the same group as <paramref name="entity"/>, including itself.</summary>
    public static IQueryable<T> WithSiblings<T>(this IQueryable<T> query, T entity)
        where T : IVersionGrouped
    {
        ArgumentNullException.ThrowIfNull(entity);
        var versionGroupId = entity.VersionGroupId;
        return query.Where(e => e.VersionGroupId == versionGroupId);
    }

    /// <summary>Records in the group that carry <paramref name="version"/>; <paramref name="exceptId"/> leaves out the record being changed.</summary>
    // RULE VSN-8
    public static IQueryable<T> WithVersion<T>(this IQueryable<T> query, Guid versionGroupId, string version, Guid? exceptId = null)
        where T : IVersionGrouped
    {
        var label = VersionGroupOperations.NormalizeVersion(version);
        return query.Where(e => e.VersionGroupId == versionGroupId && e.Version == label && (exceptId == null || e.Id != exceptId));
    }

    public static IQueryable<IGrouping<Guid, T>> GroupByVersionGroup<T>(this IQueryable<T> query)
        where T : IVersionGrouped
    {
        return query.GroupBy(e => e.VersionGroupId);
    }
}
