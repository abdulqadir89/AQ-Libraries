using System.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AQ.EntityFramework.SaveChanges;

internal static class OwnedChangeTouchStep
{
    // EF leaves an owner Unchanged when only an owned type (value object, VerificationState, periodic review state, ...) changed, so
    // lifecycle, audit and snapshot steps would skip that save. Flagging Revision re-marks the owner Modified.
    // RULE EFC-4
    public static void Run(DbContext context)
    {
        var all = context.ChangeTracker.Entries().ToList();

        if (!all.Any(e => e.Metadata.IsOwned() && IsPending(e)))
        {
            return;
        }

        var entries = all
            .Where(e => e.State == EntityState.Unchanged
                && e.Metadata.FindProperty("Revision") is not null
                && e.Metadata.GetNavigations().Any(n => n.ForeignKey.IsOwnership && !n.IsOnDependent))
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        // A cleared owned reference or a removed owned collection item is no longer reachable from the owner's navigations.
        var deletedOwned = all.Where(e => e.State == EntityState.Deleted && e.Metadata.IsOwned()).ToList();
        var ownersOfDeleted = new HashSet<object>(ReferenceEqualityComparer.Instance);

        if (deletedOwned.Count > 0)
        {
            var index = new Dictionary<(IEntityType, string), EntityEntry>();
            foreach (var e in all)
            {
                var key = e.Metadata.FindPrimaryKey();
                if (key is not null)
                {
                    index[(e.Metadata, KeyOf(key.Properties.Select(p => e.Property(p.Name).CurrentValue)))] = e;
                }
            }

            foreach (var owned in deletedOwned)
            {
                if (FindRootOwner(owned, index) is { } root)
                {
                    ownersOfDeleted.Add(root.Entity);
                }
            }
        }

        foreach (var entry in entries)
        {
            if (ownersOfDeleted.Contains(entry.Entity) || OwnedChanged(context, entry))
            {
                entry.Property("Revision").IsModified = true;
            }
        }
    }

    private static EntityEntry? FindRootOwner(EntityEntry owned, Dictionary<(IEntityType, string), EntityEntry> index)
    {
        var current = owned;

        while (current.Metadata.FindOwnership() is { } ownership)
        {
            var foreignKey = KeyOf(ownership.Properties.Select(p => current.Property(p.Name).CurrentValue));

            if (!index.TryGetValue((ownership.PrincipalEntityType, foreignKey), out var principal))
            {
                return null;
            }

            current = principal;
        }

        return current;
    }

    private static string KeyOf(IEnumerable<object?> values) => string.Join('\u001f', values.Select(v => v?.ToString()));

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
        entry.State is EntityState.Added or EntityState.Deleted or EntityState.Modified;
}
