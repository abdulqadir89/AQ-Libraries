using AQ.Entities;
using AQ.EntityFramework.Extensions;
using AQ.Utilities.Verification;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.SaveChanges;

internal static class VerificationResetStep
{
    // RULE EFC-7
    public static void Run(DbContext context)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified && e.Entity is IVerifiable)
            .Where(e => VerifiableAttributeResolver.HasVerifiableProperties(e.Entity.GetType()))
            .ToList();

        foreach (var entry in entries)
        {
            var changed = VerifiableAttributeResolver.AnyVerifiablePropertyModified(
                entry.Entity.GetType(),
                propertyName => entry.IsPropertyModified(propertyName));

            if (!changed)
            {
                continue;
            }

            ((IVerifiable)entry.Entity).Verification.Reset();
        }
    }
}
