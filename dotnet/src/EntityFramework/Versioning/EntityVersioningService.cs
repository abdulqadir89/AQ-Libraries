using AQ.Abstractions;
using AQ.Utilities.EntityData;
using AQ.Utilities.Versioning;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Versioning;

public sealed class EntityVersioningService
{
    public Task<EntityVersion<TUser>> CreateFromCurrentAsync<TUser>(DbContext db, object entity, string? label, CancellationToken cancellationToken = default)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(entity);
        var descriptor = VersionRegistry.TryGet(entity.GetType())
            ?? throw new InvalidOperationException($"{entity.GetType().Name} is not versionable.");
        var entityId = entity is IEntity identified
            ? identified.Id
            : throw new InvalidOperationException($"{entity.GetType().Name} does not implement IEntity.");
        var data = EntityDataRegistry<VersionedAttribute>.Serialize(entity);
        return CreateAsync<TUser>(db, descriptor, entityId, label, data, null, cancellationToken);
    }

    public Task<EntityVersion<TUser>> CreateFromVersionAsync<TUser>(DbContext db, EntityVersion<TUser> source, string? label, CancellationToken cancellationToken = default)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(source);
        var descriptor = VersionRegistry.TryGet(source.GetType())
            ?? throw new InvalidOperationException($"{source.GetType().Name} is not a registered version type.");
        return CreateAsync<TUser>(db, descriptor, source.GetEntityId(), label, source.Data, source.VersionNumber, cancellationToken);
    }

    public IQueryable<EntityVersion<TUser>> Query<TUser>(DbContext db, string entityTypeName, Guid entityId) where TUser : class
    {
        ArgumentNullException.ThrowIfNull(db);
        var descriptor = VersionRegistry.TryGet(entityTypeName)
            ?? throw new InvalidOperationException($"{entityTypeName} is not versionable.");
        return (IQueryable<EntityVersion<TUser>>)descriptor.Query(new DbContextQueryableSource(db), entityId);
    }

    private static async Task<EntityVersion<TUser>> CreateAsync<TUser>(
        DbContext db,
        VersionDescriptor descriptor,
        Guid entityId,
        string? label,
        string data,
        int? sourceVersionNumber,
        CancellationToken cancellationToken) where TUser : class
    {
        var source = new DbContextQueryableSource(db);
        for (var attempt = 0; ; attempt++)
        {
            var existing = (IQueryable<EntityVersion<TUser>>)descriptor.Query(source, entityId);
            var next = (await existing.MaxAsync(v => (int?)v.VersionNumber, cancellationToken) ?? 0) + 1;
            var version = (EntityVersion<TUser>)descriptor.Factory(entityId, next, label, data, sourceVersionNumber);
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
