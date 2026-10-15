using AQ.Actors;
using AQ.Entities;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.SaveChanges;

internal static class AuditStep
{
    // RULE EFC-3
    public static void Run(DbContext context, Actor? actor)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified) && e.Entity is ITimestamped)
            .ToList();

        foreach (var entry in entries)
        {
            if (entry.Entity is IAuditable auditable)
            {
                // Lifecycle hooks may have added auditable entities after the actor check.
                var principalId = (actor ?? throw new MissingActorException()).PrincipalId;

                if (entry.State == EntityState.Added)
                {
                    auditable.SetCreatedBy(principalId);
                    entry.Property("CreatedAt").IsModified = true;
                    entry.Property("CreatedById").IsModified = true;
                    entry.Property("UpdatedById").IsModified = true;
                    entry.Property("Revision").IsModified = true;
                }
                else
                {
                    auditable.SetUpdatedBy(principalId);
                    entry.Property("UpdatedAt").IsModified = true;
                    entry.Property("UpdatedById").IsModified = true;
                    entry.Property("Revision").IsModified = true;
                }
            }
            else if (entry.Entity is Entity timestamped)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property("CreatedAt").IsModified = true;
                    entry.Property("Revision").IsModified = true;
                }
                else
                {
                    timestamped.SetUpdatedTimestamp();
                    entry.Property("UpdatedAt").IsModified = true;
                    entry.Property("Revision").IsModified = true;
                }
            }
        }
    }
}
