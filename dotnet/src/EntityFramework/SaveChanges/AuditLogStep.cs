using System.Collections;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using AQ.Actors;
using AQ.EntityFramework.Auditing;
using AQ.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AQ.EntityFramework.SaveChanges;

/// <summary>
/// Appends an <see cref="AuditEvent"/> for every insert, update and delete of an <see cref="AuditedAttribute"/>
/// entity, in the same save. Runs after the audit and snapshot steps so recorded values are final.
/// </summary>
internal static class AuditLogStep
{
    // Maintained by other steps; recording them would put noise in every update
    private static readonly HashSet<string> Bookkeeping = ["Revision", "UpdatedAt", "UpdatedById"];

    private static readonly ConcurrentDictionary<Type, bool> AuditedTypes = new();

    private static readonly JsonSerializerOptions ValueJson = new()
    {
        Converters = { new JsonStringEnumConverter() },
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    public static void Run(DbContext context, Actor? actor)
    {
        var entries = context.ChangeTracker.Entries().ToList();

        foreach (var entry in entries.Where(e => e.Entity is AuditEvent && e.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("The audit log is append-only: an AuditEvent cannot be modified or deleted.");
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries.Where(IsAuditedChange))
        {
            if (EntityIdOf(entry) is not { } entityId)
            {
                continue;
            }

            var changes = Changes(context, entry);
            if (entry.State == EntityState.Modified && changes.Count == 0)
            {
                continue;
            }

            var operation = entry.State switch
            {
                EntityState.Added => AuditOperation.Insert,
                EntityState.Deleted => AuditOperation.Delete,
                _ => AuditOperation.Update,
            };

            context.Add(new AuditEvent(
                entry.Metadata.ClrType.Name,
                entityId,
                operation,
                JsonSerializer.Serialize(changes, ValueJson),
                actor ?? throw new MissingActorException(),
                now));
        }
    }

    /// <summary>True when the save touches an <c>[Audited]</c> entity (so an actor is needed).</summary>
    public static bool HasAuditedChange(DbContext context) => context.ChangeTracker.Entries().Any(IsAuditedChange);

    private static bool IsAuditedChange(EntityEntry entry) =>
        entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
        && !entry.Metadata.IsOwned()
        && AuditedTypes.GetOrAdd(entry.Metadata.ClrType, t => t.IsDefined(typeof(AuditedAttribute), inherit: true));

    private static Guid? EntityIdOf(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        return key is { Properties.Count: 1 } && entry.Property(key.Properties[0].Name).CurrentValue is Guid id ? id : null;
    }

    private static SortedDictionary<string, Dictionary<string, object?>> Changes(DbContext context, EntityEntry entry)
    {
        var changes = new SortedDictionary<string, Dictionary<string, object?>>(StringComparer.Ordinal);
        Collect(context, entry, prefix: string.Empty, changes);
        return changes;
    }

    private static void Collect(DbContext context, EntityEntry entry, string prefix, SortedDictionary<string, Dictionary<string, object?>> changes)
    {
        foreach (var property in entry.Properties)
        {
            var metadata = property.Metadata;
            if (metadata.IsShadowProperty() || metadata.IsPrimaryKey() || Bookkeeping.Contains(metadata.Name)
                || metadata.PropertyInfo?.IsDefined(typeof(AuditIgnoreAttribute), inherit: true) == true
                || metadata.IsForeignKey() && entry.Metadata.IsOwned())
            {
                continue;
            }

            var name = prefix + metadata.Name;
            switch (entry.State)
            {
                case EntityState.Added:
                    if (property.CurrentValue is not null)
                    {
                        changes[name] = new() { ["new"] = property.CurrentValue };
                    }

                    break;
                case EntityState.Deleted:
                    if (property.OriginalValue is not null)
                    {
                        changes[name] = new() { ["old"] = property.OriginalValue };
                    }

                    break;
                case EntityState.Modified when property.IsModified && !Equals(property.OriginalValue, property.CurrentValue):
                    changes[name] = new() { ["old"] = property.OriginalValue, ["new"] = property.CurrentValue };
                    break;
            }
        }

        // Owned types (value objects, JSON state) live in their own entries; fold their changes into the owner's
        foreach (var navigation in entry.Navigations)
        {
            if (navigation.Metadata is not INavigation { ForeignKey.IsOwnership: true, IsOnDependent: false } || navigation.CurrentValue is null)
            {
                continue;
            }

            var targets = navigation.CurrentValue is IEnumerable collection and not string
                ? collection.Cast<object>().ToList()
                : [navigation.CurrentValue];

            for (var i = 0; i < targets.Count; i++)
            {
                var suffix = navigation.CurrentValue is IEnumerable and not string ? $"[{i}]" : string.Empty;
                var ownedEntry = context.Entry(targets[i]);
                Collect(context, ownedEntry, $"{prefix}{navigation.Metadata.Name}{suffix}.", changes);
            }
        }
    }
}
