using AQ.Entities;
using AQ.Utilities.EntityData;
using AQ.Utilities.Versioning;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Versioning;

public sealed class EntityVersioningService
{
    public Task<EntityVersion<TPrincipal>> CreateFromCurrentAsync<TPrincipal>(DbContext db, object entity, string? label, CancellationToken cancellationToken = default)
        where TPrincipal : class
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(entity);
        // RULE EFC-16
        var descriptor = VersionRegistry.TryGet(entity.GetType())
            ?? throw new InvalidOperationException($"{entity.GetType().Name} is not versionable.");
        var entityId = entity is IEntity identified
            ? identified.Id
            : throw new InvalidOperationException($"{entity.GetType().Name} does not implement IEntity.");
        var data = EntityDataRegistry<VersionedAttribute>.Serialize(entity);
        return CreateAsync<TPrincipal>(db, descriptor, entityId, label, data, null, cancellationToken);
    }

    public Task<EntityVersion<TPrincipal>> CreateFromVersionAsync<TPrincipal>(DbContext db, EntityVersion<TPrincipal> source, string? label, CancellationToken cancellationToken = default)
        where TPrincipal : class
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(source);
        var descriptor = VersionRegistry.TryGet(source.GetType())
            ?? throw new InvalidOperationException($"{source.GetType().Name} is not a registered version type.");
        return CreateAsync<TPrincipal>(db, descriptor, source.GetEntityId(), label, source.Data, source.VersionNumber, cancellationToken);
    }

    public IQueryable<EntityVersion<TPrincipal>> Query<TPrincipal>(DbContext db, string entityTypeName, Guid entityId) where TPrincipal : class
    {
        ArgumentNullException.ThrowIfNull(db);
        var descriptor = VersionRegistry.TryGet(entityTypeName)
            ?? throw new InvalidOperationException($"{entityTypeName} is not versionable.");
        return (IQueryable<EntityVersion<TPrincipal>>)descriptor.Query(new DbContextQueryableSource(db), entityId);
    }

    // RULE EFC-15
    private static async Task<EntityVersion<TPrincipal>> CreateAsync<TPrincipal>(
        DbContext db,
        VersionDescriptor descriptor,
        Guid entityId,
        string? label,
        string data,
        int? sourceVersionNumber,
        CancellationToken cancellationToken) where TPrincipal : class
    {
        var source = new DbContextQueryableSource(db);
        for (var attempt = 0; ; attempt++)
        {
            var existing = (IQueryable<EntityVersion<TPrincipal>>)descriptor.Query(source, entityId);
            var next = (await existing.MaxAsync(v => (int?)v.VersionNumber, cancellationToken) ?? 0) + 1;
            var version = (EntityVersion<TPrincipal>)descriptor.Factory(entityId, next, label, data, sourceVersionNumber);
            db.Add(version);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return version;
            }
            catch (DbUpdateException) when (attempt == 0)
            {
                db.Entry(version).State = EntityState.Detached;
            }
        }
    }
}
