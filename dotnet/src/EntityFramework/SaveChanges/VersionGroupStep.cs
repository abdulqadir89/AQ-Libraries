using AQ.Utilities.Versioning;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.SaveChanges;

internal static class VersionGroupStep
{
    // RULE EFC-26
    public static void Run(DbContext context)
    {
        var entries = context.ChangeTracker.Entries<IVersionGrouped>()
            .Where(e => e.State == EntityState.Added && e.Entity.VersionGroupId == Guid.Empty)
            .ToList();

        foreach (var entry in entries)
        {
            entry.Entity.SetVersionGroup(Guid.CreateVersion7(), entry.Entity.Version);
        }
    }
}
