using System.Collections.Concurrent;
using AQ.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AQ.EntityFramework.SaveChanges;

internal static class RevisionNeutralCheck
{
    private static readonly HashSet<string> Bookkeeping = ["Revision", "UpdatedAt", "UpdatedById"];

    private static readonly ConcurrentDictionary<IEntityType, HashSet<string>> NeutralProperties = new();

    /// <summary>
    /// True when the entry is modified and every modified property is marked <see cref="RevisionNeutralAttribute"/>.
    /// A bookkeeping column flagged as modified (a touch, an owned change) asks for a bump, so it is never neutral.
    /// </summary>
    // ADR-008: a property attribute, not an atomic update or a second revision, so the save keeps its audit log and events
    public static bool IsNeutralOnly(EntityEntry entry)
    {
        if (entry.State != EntityState.Modified)
        {
            return false;
        }

        var neutral = NeutralProperties.GetOrAdd(entry.Metadata, type => type.GetProperties()
            .Where(p => p.PropertyInfo?.IsDefined(typeof(RevisionNeutralAttribute), inherit: true) == true)
            .Select(p => p.Name)
            .ToHashSet());

        if (neutral.Count == 0)
        {
            return false;
        }

        var changed = false;
        foreach (var property in entry.Properties.Where(p => p.IsModified))
        {
            var name = property.Metadata.Name;
            if (Bookkeeping.Contains(name) || !neutral.Contains(name))
            {
                return false;
            }

            changed = true;
        }

        return changed;
    }
}
