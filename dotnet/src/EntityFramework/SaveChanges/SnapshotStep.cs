using AQ.Entities;
using AQ.Utilities.EntityData;
using AQ.Utilities.Snapshots;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.SaveChanges;

internal static class SnapshotStep
{
    // RULE EFC-8
    public static void Run(DbContext context)
    {
        var entries = context.ChangeTracker.Entries<Entity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in entries)
        {
            // A revision-neutral save keeps the revision, so a snapshot of it would repeat the last one's key
            if (RevisionNeutralCheck.IsNeutralOnly(entry))
            {
                continue;
            }

            if (!SnapshotRegistry.TryGetFactory(entry.Entity.GetType(), out var factory))
            {
                continue;
            }

            context.Add(factory(entry.Entity.Id, entry.Entity.Revision, EntityDataRegistry<SnapshotAttribute>.Serialize(entry.Entity)));
        }
    }
}
