using AQ.Entities;
using AQ.Utilities.EntityData;
using AQ.Utilities.Snapshots;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.SaveChanges;

internal static class SnapshotStep
{
    public static void Run(DbContext context)
    {
        var entries = context.ChangeTracker.Entries<Entity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in entries)
        {
            if (!SnapshotRegistry.TryGetFactory(entry.Entity.GetType(), out var factory))
            {
                continue;
            }

            context.Add(factory(entry.Entity.Id, entry.Entity.Revision, EntityDataRegistry<SnapshotAttribute>.Serialize(entry.Entity)));
        }
    }
}
