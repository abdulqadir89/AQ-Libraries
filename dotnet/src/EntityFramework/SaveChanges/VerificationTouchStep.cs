using AQ.Entities;
using AQ.Utilities.Verification;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.SaveChanges;

internal static class VerificationTouchStep
{
    // EF leaves the owner Unchanged when only its owned VerificationState changed, so lifecycle, audit and
    // snapshot steps would skip a verification-only save. Flagging an audit column re-marks the owner Modified.
    public static void Run(DbContext context)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Unchanged && e.Entity is IVerifiable)
            .ToList();

        foreach (var entry in entries)
        {
            var verification = entry.Reference(nameof(IVerifiable.Verification)).TargetEntry;
            if (verification is { State: EntityState.Added or EntityState.Modified or EntityState.Deleted })
            {
                entry.Property("Revision").IsModified = true;
            }
        }
    }
}
