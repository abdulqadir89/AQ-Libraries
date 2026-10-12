using AQ.Entities;
using AQ.Utilities.Review;
using AQ.Utilities.Verification;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.SaveChanges;

internal static class VerificationTouchStep
{
    // EF leaves the owner Unchanged when only its owned VerificationState or ReviewState changed, so lifecycle, audit
    // and snapshot steps would skip that save. Flagging an audit column re-marks the owner Modified.
    public static void Run(DbContext context, bool verification, bool review)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Unchanged && ((verification && e.Entity is IVerifiable) || (review && e.Entity is IReviewable)))
            .ToList();

        foreach (var entry in entries)
        {
            if (verification && entry.Entity is IVerifiable && OwnedChanged(entry, nameof(IVerifiable.Verification))
                || review && entry.Entity is IReviewable && OwnedChanged(entry, nameof(IReviewable.Review)))
            {
                entry.Property("Revision").IsModified = true;
            }
        }
    }

    private static bool OwnedChanged(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string navigation) =>
        entry.Reference(navigation).TargetEntry is { State: EntityState.Added or EntityState.Modified or EntityState.Deleted };
}
