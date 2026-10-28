using System.Linq.Expressions;
using AQ.Utilities.Review;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AQ.EntityFramework.Review;

public static class ReviewStateMappingExtensions
{
    public const string NextReviewDueColumnName = "NextReviewDue";
    public const string LastReviewedAtColumnName = "LastReviewedAt";

    public static EntityTypeBuilder<TEntity> HasReviewState<TEntity, TState>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TState?>> property)
        where TEntity : class
        where TState : ReviewStateBase
    {
        builder.OwnsOne(property, owned =>
        {
            owned.Property(p => p.NextReviewDue).HasColumnName(NextReviewDueColumnName);
            owned.Property(p => p.LastReviewedAt).HasColumnName(LastReviewedAtColumnName);
            owned.HasIndex(p => p.NextReviewDue);
        });
        // Every column is nullable, so without this EF materializes a null Review for a never-reviewed row.
        // RULE EFC-20
        builder.Navigation(property).IsRequired();
        return builder;
    }
}
