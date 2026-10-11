using AQ.Abstractions;
using AQ.EntityFramework.Extensions;
using AQ.Utilities.Verification;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.SaveChanges;

internal static class VerificationResetStep
{
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

            if (changed)
            {
                ((IVerifiable)entry.Entity).Verification.Reset();
            }
        }
    }
}
