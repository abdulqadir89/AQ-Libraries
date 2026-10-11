using AQ.Entities;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.SaveChanges;

internal static class LifecycleStep
{
    public static void Run(DbContext context)
    {
        var entries = context.ChangeTracker.Entries<Entity>().ToList();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.OnAdd();
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.OnUpdate();
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.Entity.OnRemove();
            }

            if (entry.State is EntityState.Added or EntityState.Modified && entry.Entity is IHasActiveFlag effectiveEntity)
            {
                effectiveEntity.EvaluateIsActive();
            }
        }
    }
}
