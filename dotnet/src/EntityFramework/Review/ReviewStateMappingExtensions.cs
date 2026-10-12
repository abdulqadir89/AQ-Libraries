using System.Linq.Expressions;
using AQ.Utilities.Review;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AQ.EntityFramework.Review;

public static class ReviewStateMappingExtensions
{
    public const string NextReviewDueColumnName = "NextReviewDue";
    public const string LastReviewedAtColumnName = "LastReviewedAt";
    public const string LastReviewedByIdColumnName = "LastReviewedById";
    public const string LastReviewActivityIdColumnName = "LastReviewActivityId";
    public const string LastNotifiedAtColumnName = "LastReviewNotifiedAt";

    public static EntityTypeBuilder<TEntity> HasReviewState<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, ReviewState>> property) where TEntity : class
    {
        builder.OwnsOne(property, owned =>
        {
            owned.Property(p => p.NextReviewDue).HasColumnName(NextReviewDueColumnName);
            owned.Property(p => p.LastReviewedAt).HasColumnName(LastReviewedAtColumnName);
            owned.Property(p => p.LastReviewedById).HasColumnName(LastReviewedByIdColumnName);
            owned.Property(p => p.LastReviewActivityId).HasColumnName(LastReviewActivityIdColumnName);
            owned.Property(p => p.LastNotifiedAt).HasColumnName(LastNotifiedAtColumnName);
            owned.HasIndex(p => p.NextReviewDue);
        });
        // Every column is nullable, so without this EF materializes a null Review for a never-reviewed row.
        builder.Navigation(property).IsRequired();
        return builder;
    }
}
