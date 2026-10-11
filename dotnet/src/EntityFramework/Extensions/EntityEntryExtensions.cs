using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AQ.EntityFramework.Extensions;

public static class EntityEntryExtensions
{
    /// <summary>
    /// Reports whether <paramref name="propertyName"/> was modified on <paramref name="entry"/>, handling both
    /// regular scalar properties and owned-type reference navigations (e.g. OwnsOne value objects), which EF
    /// tracks as modified on the owned entity's own entry rather than as an IsModified flag on the parent.
    /// </summary>
    public static bool IsPropertyModified(this EntityEntry entry, string propertyName)
    {
        var navigation = entry.Metadata.FindNavigation(propertyName);
        if (navigation is not null && navigation.TargetEntityType.IsOwned())
        {
            var targetEntry = entry.Reference(propertyName).TargetEntry;
            return targetEntry is not null && targetEntry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;
        }

        return entry.Property(propertyName).IsModified;
    }
}
