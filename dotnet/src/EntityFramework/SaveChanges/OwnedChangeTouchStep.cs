using System.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AQ.EntityFramework.SaveChanges;

internal static class OwnedChangeTouchStep
{
    // EF leaves an owner Unchanged when only an owned type (value object, VerificationState, ReviewState, ...) changed, so
    // lifecycle, audit and snapshot steps would skip that save. Flagging Revision re-marks the owner Modified.
    public static void Run(DbContext context)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Unchanged && e.Metadata.FindProperty("Revision") is not null)
            .ToList();

        foreach (var entry in entries)
        {
            if (OwnedChanged(context, entry))
            {
                entry.Property("Revision").IsModified = true;
            }
        }
    }

    private static bool OwnedChanged(DbContext context, EntityEntry owner)
    {
        foreach (var navigation in owner.Navigations)
        {
            if (navigation.Metadata is not INavigation { ForeignKey.IsOwnership: true, IsOnDependent: false } || navigation.CurrentValue is null)
            {
                continue;
            }

            var targets = navigation.CurrentValue is IEnumerable collection and not string
                ? collection.Cast<object>()
                : [navigation.CurrentValue];

            if (targets.Any(target => IsPending(context.Entry(target)) || OwnedChanged(context, context.Entry(target))))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPending(EntityEntry entry) =>
        entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;
}
