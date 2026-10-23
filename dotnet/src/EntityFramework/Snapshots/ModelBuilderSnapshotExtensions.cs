using AQ.Utilities.Snapshots;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Snapshots;

public static class ModelBuilderSnapshotExtensions
{
    public const string DiscriminatorName = "EntityType";

    public static ModelBuilder ApplyEntitySnapshots(this ModelBuilder modelBuilder)
    {
        var root = modelBuilder.Entity<EntitySnapshot>();
        root.ToTable("EntitySnapshots");
        root.Property(s => s.Data).IsRequired();
        var discriminator = root.HasDiscriminator(DiscriminatorName, typeof(string));
        root.Property(DiscriminatorName).HasMaxLength(100);
        // RULE EFC-21
        root.HasIndex(nameof(EntitySnapshot.EntityId), nameof(EntitySnapshot.Version)).IsUnique();
        root.HasIndex(DiscriminatorName, nameof(EntitySnapshot.CreatedAt));

        foreach (var descriptor in SnapshotRegistry.All)
        {
            discriminator.HasValue(descriptor.SnapshotType, descriptor.EntityTypeName);

            var snapshot = modelBuilder.Entity(descriptor.SnapshotType);
            snapshot.HasBaseType(typeof(EntitySnapshot));
            // RULE EFC-22
            snapshot.HasOne(descriptor.EntityType, descriptor.NavigationPropertyName)
                .WithMany()
                .HasForeignKey(descriptor.ForeignKeyPropertyName)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }

        return modelBuilder;
    }
}
