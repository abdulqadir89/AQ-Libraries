using AQ.Utilities.Tagging;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.SaveChanges;

internal static class TagLinkStep
{
    // RULE EFC-30
    public static void Run(DbContext context)
    {
        var discovered = context.ChangeTracker.Entries<IEntityTag>()
            .Where(e => e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in discovered)
        {
            entry.State = EntityState.Added;
        }
    }
}
