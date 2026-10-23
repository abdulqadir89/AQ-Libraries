using AQ.Utilities.Versioning;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Versioning;

public static class ModelBuilderVersionGroupExtensions
{
    /// <summary>Configures every version-grouped entity type in the model; call it after the entity types are registered.</summary>
    public static ModelBuilder ApplyVersionGroups(this ModelBuilder modelBuilder)
    {
        var grouped = modelBuilder.Model.GetEntityTypes()
            .Where(t => !t.IsOwned()
                && typeof(IVersionGrouped).IsAssignableFrom(t.ClrType)
                && (t.BaseType is null || !typeof(IVersionGrouped).IsAssignableFrom(t.BaseType.ClrType)))
            .Select(t => t.ClrType)
            .ToList();

        foreach (var clrType in grouped)
        {
            var entity = modelBuilder.Entity(clrType);
            entity.Property(nameof(IVersionGrouped.VersionGroupId)).IsRequired();
            entity.Property(nameof(IVersionGrouped.Version)).HasMaxLength(VersionGroupOperations.MaxVersionLength);
            // RULE EFC-25
            entity.HasIndex(nameof(IVersionGrouped.VersionGroupId), nameof(IVersionGrouped.Version)).IsUnique();
        }

        return modelBuilder;
    }
}
