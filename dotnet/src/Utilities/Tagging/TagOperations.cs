namespace AQ.Utilities.Tagging;

public static class TagOperations
{
    /// <summary>Makes the entity carry exactly <paramref name="tagIds"/>: tags it has and the set lacks are removed, tags the set has and it lacks are added.</summary>
    // RULE TAG-1
    public static void SetTags<T>(this T entity, IEnumerable<Guid> tagIds)
        where T : ITaggable
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(tagIds);

        var wanted = tagIds.Where(id => id != Guid.Empty).ToHashSet();
        var current = entity.TagIds.ToHashSet();

        foreach (var removed in current.Except(wanted))
        {
            entity.RemoveTagId(removed);
        }

        foreach (var added in wanted.Except(current))
        {
            entity.AddTagId(added);
        }
    }

    /// <summary>Adds the tags the entity does not carry yet and leaves the others alone.</summary>
    // RULE TAG-2
    public static void AddTags<T>(this T entity, IEnumerable<Guid> tagIds)
        where T : ITaggable
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(tagIds);

        var current = entity.TagIds.ToHashSet();
        foreach (var tagId in tagIds.Where(id => id != Guid.Empty && !current.Contains(id)).Distinct())
        {
            entity.AddTagId(tagId);
        }
    }

    public static void RemoveTags<T>(this T entity, IEnumerable<Guid> tagIds)
        where T : ITaggable
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(tagIds);

        var current = entity.TagIds.ToHashSet();
        foreach (var tagId in tagIds.Where(current.Contains).Distinct())
        {
            entity.RemoveTagId(tagId);
        }
    }
}
