using AQ.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AQ.EntityFramework.Extensions;

public static class RevisionConcurrencyExtensions
{
    /// <summary>
    /// Makes the revision of an entity type a database concurrency token: an update or delete only goes through
    /// when the row still has the revision the entity was loaded with, or the one set by <see cref="ExpectRevision"/>.
    /// </summary>
    // RULE CON-3
    public static void UseRevisionConcurrencyToken<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ITimestamped
    {
        builder.Property(nameof(ITimestamped.Revision)).IsConcurrencyToken();
    }

    /// <summary>
    /// Replaces the revision the next save compares against with the one the caller was looking at, so a save from
    /// an out-of-date view fails with a concurrency exception even when the row changed before it was loaded here.
    /// </summary>
    // RULE CON-4
    public static void ExpectRevision<TEntity>(this DbContext context, TEntity entity, int revision)
        where TEntity : class, ITimestamped
    {
        context.Entry(entity).Property(nameof(ITimestamped.Revision)).OriginalValue = revision;
    }
}
