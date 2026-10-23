using AQ.Utilities.Tagging;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Tagging;

public static class ModelBuilderTagExtensions
{
    public const string DiscriminatorName = "EntityType";

    public static ModelBuilder ApplyEntityTags<TTag>(this ModelBuilder modelBuilder) where TTag : class, ITag
    {
        var descriptors = TagRegistry.All.Where(d => d.TagType == typeof(TTag)).ToList();
        if (descriptors.Count == 0)
        {
            return modelBuilder;
        }

        var root = modelBuilder.Entity<EntityTag<TTag>>();
        root.ToTable("EntityTags");
        var discriminator = root.HasDiscriminator(DiscriminatorName, typeof(string));
        root.Property(DiscriminatorName).HasMaxLength(100);
        // RULE EFC-27
        root.HasIndex(nameof(EntityTag<TTag>.EntityId), nameof(EntityTag<TTag>.TagId)).IsUnique();
        root.HasIndex(nameof(EntityTag<TTag>.TagId));
        // RULE EFC-28
        root.HasOne(l => l.Tag)
            .WithMany()
            .HasForeignKey(l => l.TagId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        foreach (var descriptor in descriptors)
        {
            discriminator.HasValue(descriptor.LinkType, descriptor.EntityTypeName);

            var link = modelBuilder.Entity(descriptor.LinkType);
            link.HasBaseType(typeof(EntityTag<TTag>));
            // RULE EFC-29
            link.HasOne(descriptor.EntityType, descriptor.NavigationPropertyName)
                .WithMany(descriptor.CollectionPropertyName)
                .HasForeignKey(descriptor.ForeignKeyPropertyName)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }

        return modelBuilder;
    }
}
