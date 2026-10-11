using AQ.Entities;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.SaveChanges;

internal static class AuditStep
{
    public static void Run(DbContext context, Guid? currentUserId)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified) && e.Entity is ITimestamped)
            .ToList();

        foreach (var entry in entries)
        {
            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.SetCreatedBy(currentUserId);
                    entry.Property("CreatedAt").IsModified = true;
                    entry.Property("CreatedById").IsModified = true;
                    entry.Property("Revision").IsModified = true;
                }
                else
                {
                    auditable.SetUpdatedBy(currentUserId);
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
