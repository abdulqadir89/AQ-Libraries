using AQ.Entities;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Extensions;

public static class RevisionConcurrencyExtensions
{
    /// <summary>
    /// Makes the revision of each given entity type a database concurrency token: an update or delete then only goes
    /// through when the row still has the revision the entity was loaded with, or the one set by
    /// <see cref="ExpectRevision"/>. For a type in an inheritance hierarchy the token is set on the hierarchy's root,
    /// which owns the column. Call after the entity types are configured.
    /// </summary>
    // RULE CON-3
    public static ModelBuilder UseRevisionConcurrencyToken(this ModelBuilder modelBuilder, params Type[] entityTypes)
    {
        foreach (var type in entityTypes)
        {
            var entityType = modelBuilder.Model.FindEntityType(type)
                ?? throw new InvalidOperationException($"{type.Name} is not an entity type of the model.");
            if (!typeof(ITimestamped).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"{type.Name} has no revision, so it cannot use a revision concurrency token.");
            }

            modelBuilder.Entity(entityType.GetRootType().ClrType).Property(nameof(ITimestamped.Revision)).IsConcurrencyToken();
        }

        return modelBuilder;
    }

    /// <summary>
    /// Replaces the revision the next save compares against with the one the caller was looking at, so a save from
    /// an out-of-date view fails with a concurrency exception even when the row changed before it was loaded here.
    /// </summary>
    // RULE CON-4
    public static void ExpectRevision<TEntity>(this DbContext context, TEntity entity, int revision)
        where TEntity : class, ITimestamped
    {
        var property = context.Entry(entity).Property(nameof(ITimestamped.Revision));
        if (!property.Metadata.IsConcurrencyToken)
        {
            throw new InvalidOperationException($"{entity.GetType().Name} does not use a revision concurrency token, so a stale save would not be refused.");
        }

        property.OriginalValue = revision;
    }

    /// <summary>
    /// Runs <paramref name="operation"/> (load, change) and saves, and when the save loses a race on a concurrency
    /// token starts over from a clean change tracker, so the operation sees the winner's values. For writers that
    /// no person is waiting on (background jobs, event handlers): the operation must load what it changes itself and
    /// must not rely on tracked state from before the call, because the tracker is cleared before every retry.
    /// The last concurrency exception is rethrown after <paramref name="maxAttempts"/> attempts.
    /// </summary>
    // RULE CON-6
    public static async Task SaveChangesWithRetryAsync(
        this DbContext context,
        Func<CancellationToken, Task> operation,
        int maxAttempts = 3,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await operation(cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                context.ChangeTracker.Clear();
            }
        }
    }

    /// <summary>
    /// Marks an entity as updated even though none of its own values changed, so the next save stamps it and raises
    /// its revision. For an edit that only replaces the entity's child rows (a full-set replace), so the revision the
    /// caller was looking at moves on and a second caller working from the old view is refused.
    /// </summary>
    // RULE CON-5
    public static void TouchRevision<TEntity>(this DbContext context, TEntity entity)
        where TEntity : class, ITimestamped
    {
        var entry = context.Entry(entity);
        if (entry.State is EntityState.Unchanged)
        {
            entry.Property(nameof(ITimestamped.UpdatedAt)).IsModified = true;
        }
    }
}
