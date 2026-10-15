using AQ.Entities;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Hierarchy;

public class HierarchyService<TContext, T, TClosure>(TContext db) : IHierarchyService<T>
    where TContext : DbContext
    where T : class, IHierarchicalEntity
    where TClosure : class, IHierarchicalEntityClosure, new()
{

    // RULE EFC-18
    public async Task UpdateClosureOnParentChangeAsync(T entity, Guid? newParentId)
    {
        if (entity.ParentId == newParentId)
        {
            // For new entities (not yet persisted), closure records don't exist yet,
            // so we must still build the initial self-reference closure.
            var hasClosure = await db.Set<TClosure>()
                .AnyAsync(c => c.AncestorId == entity.Id && c.DescendantId == entity.Id);
            if (hasClosure) return;
        }

        if (await CircularReferenceExists(entity, newParentId))
            throw new InvalidOperationException("Cannot set a descendant as the parent (circular reference).");

        var closures = db.Set<TClosure>();

        // Get all descendants of the entity (nodes in the subtree)
        var descendantIds = await closures
            .Where(c => c.AncestorId == entity.Id)
            .Select(c => c.DescendantId)
            .ToListAsync();

        // Include the entity itself if it doesn't exist in descendants
        if (!descendantIds.Contains(entity.Id))
            descendantIds.Add(entity.Id);

        // Get all ancestors of the entity (old parent path)
        var ancestorIds = await closures
            .Where(c => c.DescendantId == entity.Id)
            .Select(c => c.AncestorId)
            .ToListAsync();

        // Remove all closure records for the subtree being moved
        var toRemove = await closures
            .Where(c => ancestorIds.Contains(c.AncestorId) && descendantIds.Contains(c.DescendantId))
            .ToListAsync();

        closures.RemoveRange(toRemove);

        var toInsert = new List<TClosure>
        {
            // Re-insert self-reference for the entity
            new TClosure
            {
                AncestorId = entity.Id,
                DescendantId = entity.Id
            }
        };

        // Re-insert entity to its descendants relationships
        foreach (var descendantId in descendantIds.Where(d => d != entity.Id))
        {
            toInsert.Add(new TClosure
            {
                AncestorId = entity.Id,
                DescendantId = descendantId
            });
        }

        // Add relationships to new parent hierarchy
        if (newParentId.HasValue)
        {
            var newAncestors = await closures
                .Where(c => c.DescendantId == newParentId.Value)
                .Select(c => c.AncestorId)
                .ToListAsync();

            // Add the new parent itself as an ancestor
            if (!newAncestors.Contains(newParentId.Value))
                newAncestors.Add(newParentId.Value);

            // Create relationships from all new ancestors to all descendants in the subtree
            foreach (var ancestorId in newAncestors)
            {
                foreach (var descendantId in descendantIds)
                {
                    toInsert.Add(new TClosure
                    {
                        AncestorId = ancestorId,
                        DescendantId = descendantId
                    });
                }
            }
        }

        // Add all new closure records
        if (toInsert.Count > 0)
            await closures.AddRangeAsync(toInsert);
    }

    public async Task<bool> CircularReferenceExists(T entity, Guid? newParentId)
    {
        if (!newParentId.HasValue)
            return false;

        // Check if the new parent is a descendant of the entity
        return await db.Set<TClosure>()
            .AnyAsync(c => c.AncestorId == entity.Id && c.DescendantId == newParentId.Value);
    }

    public async Task<bool> IsDescendantAsync(T entity, Guid ancestorId)
    {
        return await db.Set<TClosure>()
            .AnyAsync(c => c.AncestorId == ancestorId && c.DescendantId == entity.Id);
    }

    public async Task<bool> IsAncestorAsync(T entity, Guid descendantId)
    {
        return await db.Set<TClosure>()
            .AnyAsync(c => c.AncestorId == entity.Id && c.DescendantId == descendantId);
    }

    public async Task RemoveClosureForEntityAsync(Guid entityId)
    {
        var closures = db.Set<TClosure>();

        var toRemove = await closures
            .Where(c => c.AncestorId == entityId || c.DescendantId == entityId)
            .ToListAsync();

        closures.RemoveRange(toRemove);
    }

    // RULE EFC-19
    public async Task<int> RecomputeClosureAsync(Guid? entityId = null)
    {
        var closures = db.Set<TClosure>();
        var entities = db.Set<T>();

        if (entityId.HasValue)
        {
            // Collect the subtree rooted at entityId by traversing ParentId links
            var allEntities = await entities.ToListAsync();
            var subtreeIds = CollectSubtree(entityId.Value, allEntities);

            // Remove all closure rows touching this subtree
            var toRemove = await closures
                .Where(c => subtreeIds.Contains(c.AncestorId) || subtreeIds.Contains(c.DescendantId))
                .ToListAsync();
            closures.RemoveRange(toRemove);

            // Rebuild: get the entity's ancestor chain from the (still-valid) parent closure rows
            var entity = allEntities.First(e => e.Id == entityId.Value);
            var parentAncestorIds = entity.ParentId.HasValue
                ? await closures
                    .Where(c => c.DescendantId == entity.ParentId.Value)
                    .Select(c => c.AncestorId)
                    .ToListAsync()
                : [];

            if (entity.ParentId.HasValue && !parentAncestorIds.Contains(entity.ParentId.Value))
                parentAncestorIds.Add(entity.ParentId.Value);

            var subtreeEntities = allEntities.Where(e => subtreeIds.Contains(e.Id)).ToList();
            var toInsert = BuildClosureForSubtree(subtreeEntities, entityId.Value, parentAncestorIds);

            await closures.AddRangeAsync(toInsert);
            return toInsert.Count;
        }
        else
        {
            // Full recompute: wipe everything and rebuild from scratch
            var toRemove = await closures.ToListAsync();
            closures.RemoveRange(toRemove);

            var allEntities = await entities.ToListAsync();
            var toInsert = new List<TClosure>();

            // Process in topological order (roots first)
            var ordered = TopologicalSort(allEntities);

            // Track inserted ancestor sets per node as we go
            var ancestorMap = new Dictionary<Guid, List<Guid>>();

            foreach (var entity in ordered)
            {
                var ancestors = entity.ParentId.HasValue && ancestorMap.TryGetValue(entity.ParentId.Value, out var parentAncestors)
                    ? [.. parentAncestors, entity.ParentId.Value]
                    : new List<Guid>();

                ancestorMap[entity.Id] = ancestors;

                // Self-reference
                toInsert.Add(new TClosure { AncestorId = entity.Id, DescendantId = entity.Id });

                // Ancestor → this node
                foreach (var ancestorId in ancestors)
                    toInsert.Add(new TClosure { AncestorId = ancestorId, DescendantId = entity.Id });
            }

            await closures.AddRangeAsync(toInsert);
            return toInsert.Count;
        }
    }

    private static HashSet<Guid> CollectSubtree(Guid rootId, List<T> allEntities)
    {
        var result = new HashSet<Guid> { rootId };
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var child in allEntities.Where(e => e.ParentId == current))
            {
                if (result.Add(child.Id))
                    queue.Enqueue(child.Id);
            }
        }

        return result;
    }

    private static List<TClosure> BuildClosureForSubtree(List<T> subtreeEntities, Guid rootId, List<Guid> rootAncestorIds)
    {
        var toInsert = new List<TClosure>();
        var ancestorMap = new Dictionary<Guid, List<Guid>>();

        var ordered = TopologicalSort(subtreeEntities);

        foreach (var entity in ordered)
        {
            List<Guid> ancestors;
            if (entity.Id == rootId)
            {
                ancestors = rootAncestorIds;
            }
            else
            {
                ancestors = entity.ParentId.HasValue && ancestorMap.TryGetValue(entity.ParentId.Value, out var parentAncestors)
                    ? [.. parentAncestors, entity.ParentId.Value]
                    : new List<Guid>();
            }

            ancestorMap[entity.Id] = ancestors;

            toInsert.Add(new TClosure { AncestorId = entity.Id, DescendantId = entity.Id });

            foreach (var ancestorId in ancestors)
                toInsert.Add(new TClosure { AncestorId = ancestorId, DescendantId = entity.Id });
        }

        return toInsert;
    }

    private static List<T> TopologicalSort(List<T> entities)
    {
        var entityMap = entities.ToDictionary(e => e.Id);
        var result = new List<T>();
        var visited = new HashSet<Guid>();

        void Visit(T entity)
        {
            if (!visited.Add(entity.Id)) return;
            if (entity.ParentId.HasValue && entityMap.TryGetValue(entity.ParentId.Value, out var parent))
                Visit(parent);
            result.Add(entity);
        }

        foreach (var entity in entities)
            Visit(entity);

        return result;
    }
}
