using AQ.Entities;
using AQ.Utilities.Commenting;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Commenting;

public static class ModelBuilderCommentExtensions
{
    public const string DiscriminatorName = "EntityType";

    public static ModelBuilder ApplyEntityComments<TComment>(this ModelBuilder modelBuilder) where TComment : class, IComment
    {
        var descriptors = CommentRegistry.All.Where(d => d.BaseCommentType == typeof(TComment)).ToList();
        if (descriptors.Count == 0)
        {
            return modelBuilder;
        }

        var root = modelBuilder.Entity<TComment>();
        root.ToTable("EntityComments");
        var discriminator = root.HasDiscriminator(DiscriminatorName, typeof(string));
        root.Property(DiscriminatorName).HasMaxLength(100);
        root.HasIndex(nameof(IComment.ParentId));
        // NoAction, not Restrict: Restrict is checked per row, so it would block the cascade from the record that removes a comment and its replies together.
        // RULE EFC-31
        root.HasOne<TComment>()
            .WithMany()
            .HasForeignKey(nameof(IComment.ParentId))
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        foreach (var descriptor in descriptors)
        {
            discriminator.HasValue(descriptor.CommentType, descriptor.EntityTypeName);

            var comment = modelBuilder.Entity(descriptor.CommentType);
            comment.HasBaseType(typeof(TComment));
            // RULE EFC-32
            comment.HasOne(descriptor.EntityType, descriptor.NavigationPropertyName)
                .WithMany()
                .HasForeignKey(descriptor.ForeignKeyPropertyName)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }

        return modelBuilder;
    }

    /// <summary>Maps the app's vote type: one row per voter and comment, removed together with its comment.</summary>
    public static ModelBuilder ApplyCommentVotes<TVote, TComment>(this ModelBuilder modelBuilder)
        where TVote : class, IEntity
        where TComment : class, IComment
    {
        var vote = modelBuilder.Entity<TVote>();
        vote.ToTable("EntityCommentVotes");
        // RULE EFC-33
        vote.HasIndex(nameof(CommentVote<object>.CommentId), nameof(IAuditable.CreatedById)).IsUnique();
        // RULE EFC-33
        vote.HasOne<TComment>()
            .WithMany()
            .HasForeignKey(nameof(CommentVote<object>.CommentId))
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        return modelBuilder;
    }
}
