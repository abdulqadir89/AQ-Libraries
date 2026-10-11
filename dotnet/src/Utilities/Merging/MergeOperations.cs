using AQ.Abstractions;

namespace AQ.Utilities.Merging;

public static class MergeOperations
{
    public static void Merge<T>(this T primary, IEnumerable<T> children)
        where T : IEntity, IMergeable<T>
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(children);

        if (!primary.IsPrimary)
        {
            throw new InvalidOperationException($"Entity {primary.Id} is already merged into {primary.PrimaryId} and cannot be a merge target.");
        }

        var distinct = children.DistinctBy(c => c.Id).ToList();

        foreach (var child in distinct)
        {
            if (child.Id == primary.Id)
            {
                throw new InvalidOperationException("An entity cannot be merged into itself.");
            }

            if (!child.IsPrimary)
            {
                throw new InvalidOperationException($"Entity {child.Id} is already merged into {child.PrimaryId}.");
            }
        }

        foreach (var child in distinct)
        {
            child.SetPrimaryId(primary.Id);
        }
    }

    public static void Unmerge<T>(this T child)
        where T : IEntity, IMergeable<T>
    {
        ArgumentNullException.ThrowIfNull(child);

        if (child.IsPrimary)
        {
            throw new InvalidOperationException($"Entity {child.Id} is not merged into another entity.");
        }

        child.SetPrimaryId(null);
    }
}
