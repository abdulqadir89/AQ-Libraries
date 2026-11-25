using AQ.Utilities.Attachments;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Attachments;

public static class ModelBuilderAttachmentExtensions
{
    public const string DiscriminatorName = "EntityType";

    public static ModelBuilder ApplyEntityAttachments<TAttachment>(this ModelBuilder modelBuilder) where TAttachment : class, IAttachment
    {
        var descriptors = AttachmentRegistry.All.Where(d => d.BaseAttachmentType == typeof(TAttachment)).ToList();
        if (descriptors.Count == 0)
        {
            return modelBuilder;
        }

        var root = modelBuilder.Entity<TAttachment>();
        root.ToTable("EntityAttachments");
        var discriminator = root.HasDiscriminator(DiscriminatorName, typeof(string));
        root.Property(DiscriminatorName).HasMaxLength(100);
        root.Property(a => a.FileName).HasMaxLength(260);
        root.Property(a => a.ContentType).HasMaxLength(255);
        // RULE EFC-35
        root.HasIndex(DiscriminatorName, nameof(IAttachment.OwnerId), nameof(IAttachment.FileName)).IsUnique();
        root.HasIndex(nameof(IAttachment.IsPending), "CreatedAt");

        foreach (var descriptor in descriptors)
        {
            discriminator.HasValue(descriptor.AttachmentType, descriptor.EntityTypeName);

            var attachment = modelBuilder.Entity(descriptor.AttachmentType);
            attachment.HasBaseType(typeof(TAttachment));
            // RULE EFC-36
            attachment.HasOne(descriptor.EntityType, descriptor.NavigationPropertyName)
                .WithMany()
                .HasForeignKey(descriptor.ForeignKeyPropertyName)
                .IsRequired(false)
                .OnDelete(descriptor.CascadeDelete ? DeleteBehavior.Cascade : DeleteBehavior.ClientCascade);
        }

        return modelBuilder;
    }
}
