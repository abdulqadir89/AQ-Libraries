using AQ.Entities;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Extensions;

public static class TouchRevisionExtensions
{
    /// <summary>
    /// Marks an entity as updated even though none of its own values changed, so the next save stamps it and raises
    /// its revision. For an edit that only replaces the entity's child rows (a full-set replace), so the revision a
    /// caller sent with the request moves on and a second caller working from the old view is refused.
    /// </summary>
    public static void TouchRevision<TEntity>(this DbContext context, TEntity entity)
        where TEntity : class, ITimestamped
    {
        var entry = context.Entry(entity);
        if (entry.State is EntityState.Unchanged)
        {
            // RULE RVN-2
            entry.Property("UpdatedAt").IsModified = true;
        }
    }
}
