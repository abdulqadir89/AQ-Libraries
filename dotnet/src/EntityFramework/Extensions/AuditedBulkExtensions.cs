using AQ.Actors;
using AQ.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace AQ.EntityFramework.Extensions;

public static class AuditedBulkExtensions
{
    /// <summary>
    /// Bulk update that keeps audit fields honest: appends UpdatedById, UpdatedAt and Revision + 1 to the caller's
    /// setters. Use instead of <c>ExecuteUpdateAsync</c> on auditable sets, which bypass the save pipeline.
    /// </summary>
    /// <remarks>
    /// Bulk statements never reach the save pipeline, so no <see cref="AQ.EntityFramework.Auditing.AuditEvent"/> is
    /// appended. For <c>[Audited]</c> entities whose history matters, load and save the entities instead.
    /// </remarks>
    public static Task<int> ExecuteAuditedUpdateAsync<T>(
        this IQueryable<T> source,
        IActorContext actors,
        Action<UpdateSettersBuilder<T>> setters,
        CancellationToken cancellationToken = default)
        where T : class, IAuditable
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(actors);
        ArgumentNullException.ThrowIfNull(setters);

        var principalId = actors.Current.PrincipalId;
        var now = DateTimeOffset.UtcNow;

        return source.ExecuteUpdateAsync(
            builder =>
            {
                setters(builder);
                builder.SetProperty(e => e.UpdatedById, principalId);
                builder.SetProperty(e => e.UpdatedAt, (DateTimeOffset?)now);
                builder.SetProperty(e => e.Revision, e => e.Revision + 1);
            },
            cancellationToken);
    }
}
