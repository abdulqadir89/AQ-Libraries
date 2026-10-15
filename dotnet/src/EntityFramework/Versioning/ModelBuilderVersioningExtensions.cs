using AQ.Utilities.Versioning;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Versioning;

public static class ModelBuilderVersioningExtensions
{
    public const string DiscriminatorName = "EntityType";

    public static ModelBuilder ApplyEntityVersioning<TPrincipal>(this ModelBuilder modelBuilder) where TPrincipal : class
    {
        var root = modelBuilder.Entity<EntityVersion<TPrincipal>>();
        root.ToTable("EntityVersions");
        root.Property(v => v.Label).HasMaxLength(200);
        root.Property(v => v.Data).IsRequired();
        var discriminator = root.HasDiscriminator(DiscriminatorName, typeof(string));
        root.Property(DiscriminatorName).HasMaxLength(100);

        foreach (var descriptor in VersionRegistry.All)
        {
            discriminator.HasValue(descriptor.VersionType, descriptor.EntityTypeName);

            var version = modelBuilder.Entity(descriptor.VersionType);
            version.HasBaseType(typeof(EntityVersion<TPrincipal>));
            version.HasOne(descriptor.EntityType, descriptor.NavigationPropertyName)
                .WithMany()
                .HasForeignKey(descriptor.ForeignKeyPropertyName)
                .IsRequired();
            // RULE EFC-17
            version.HasIndex(descriptor.ForeignKeyPropertyName, nameof(EntityVersion<TPrincipal>.VersionNumber)).IsUnique();
        }

        return modelBuilder;
    }
}
