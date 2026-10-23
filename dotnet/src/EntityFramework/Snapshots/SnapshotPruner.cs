using AQ.Utilities.Snapshots;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Snapshots;

public sealed class SnapshotPruner(SnapshotRetentionOptions options, TimeProvider timeProvider)
{
    /// <summary>Deletes snapshots outside their retention limits; with <paramref name="dryRun"/> only counts them. Returns the count per entity type that has a limit.</summary>
    public async Task<IReadOnlyDictionary<string, int>> PruneAsync(DbContext db, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        var results = new Dictionary<string, int>(StringComparer.Ordinal);
        var now = timeProvider.GetUtcNow();

        foreach (var descriptor in SnapshotRegistry.All)
        {
            var keepLast = descriptor.KeepLast;
            var maxAgeDays = descriptor.MaxAgeDays;
            // The configuration binder copies the dictionary with an ordinal comparer, so the lookup cannot rely on the dictionary's own.
            var overrides = options.Retention.FirstOrDefault(r => string.Equals(r.Key, descriptor.EntityTypeName, StringComparison.OrdinalIgnoreCase)).Value;
            if (overrides is not null)
            {
                keepLast = overrides.KeepLast ?? keepLast;
                maxAgeDays = overrides.MaxAgeDays ?? maxAgeDays;
            }

            if (keepLast <= 0 && maxAgeDays <= 0)
            {
                continue;
            }

            var candidates = Candidates(db, descriptor.EntityTypeName, keepLast, maxAgeDays > 0 ? now.AddDays(-maxAgeDays) : null);
            if (dryRun)
            {
                results[descriptor.EntityTypeName] = await candidates.CountAsync(cancellationToken);
                continue;
            }

            results[descriptor.EntityTypeName] = await candidates.ExecuteDeleteAsync(cancellationToken);
        }

        return results;
    }

    private static IQueryable<EntitySnapshot> Candidates(DbContext db, string entityTypeName, int keepLast, DateTimeOffset? cutoff)
    {
        var all = db.Set<EntitySnapshot>();
        var query = all.Where(s => EF.Property<string>(s, ModelBuilderSnapshotExtensions.DiscriminatorName) == entityTypeName);

        // RULE EFC-24
        query = query.Where(s => all.Any(n => n.EntityId == s.EntityId && n.Version > s.Version));

        // RULE EFC-23
        if (keepLast > 0)
        {
            query = query.Where(s => all.Count(n => n.EntityId == s.EntityId && n.Version > s.Version) >= keepLast);
        }

        if (cutoff is { } limit)
        {
            query = query.Where(s => s.CreatedAt < limit);
        }

        return query;
    }
}
