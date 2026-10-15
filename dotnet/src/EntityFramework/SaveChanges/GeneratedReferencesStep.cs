using AQ.Utilities.References;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.SaveChanges;

internal static class GeneratedReferencesStep
{
    // RULE EFC-6
    public static void Run(DbContext context)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added)
            .ToList();

        foreach (var entry in entries)
        {
            ReferenceAttributeResolver.ApplyGeneratedReferences(entry.Entity);
        }
    }
}
