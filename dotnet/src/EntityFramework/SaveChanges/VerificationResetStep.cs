using AQ.Entities;
using AQ.EntityFramework.Extensions;
using AQ.Utilities.Review;
using AQ.Utilities.Verification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

            var reviewPending = IsReviewPending(entry);
            ((IVerifiable)entry.Entity).Verification.Reset();

            // A review recorded in the same save must outlive the reset, otherwise it is non-current on arrival.
            if (reviewPending && entry.Entity is IReviewable { Review.LastReviewedById: { } reviewerId } reviewable)
            {
                ((IVerifiable)entry.Entity).Verification.Add(reviewerId);
                reviewable.Review.Restamp();
            }
        }
    }

    private static bool IsReviewPending(EntityEntry entry) =>
        entry.Entity is IReviewable
        && entry.Metadata.FindNavigation(nameof(IReviewable.Review)) is not null
        && entry.Reference(nameof(IReviewable.Review)).TargetEntry is { State: EntityState.Added or EntityState.Modified } review
        && review.Property(nameof(ReviewState.LastReviewedAt)).IsModified;
}
