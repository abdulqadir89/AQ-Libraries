namespace AQ.Abstractions;

public interface IAuditable
{
    Guid? CreatedById { get; }
    DateTimeOffset CreatedAt { get; }
    Guid? UpdatedById { get; }
    DateTimeOffset? UpdatedAt { get; }
    int Revision { get; }

    /// <summary>
    /// Mark the entity as created by the given user (or null) and set CreatedAt.
    /// If the entity already has a CreatedById this call should be a no-op.
    /// </summary>
    /// <param name="userId">The user id who created the entity, or null if unauthenticated.</param>
    void SetCreatedBy(Guid? userId);

    /// <summary>
    /// Mark the entity as updated by the given user (or null) and set UpdatedAt and increment Revision.
    /// </summary>
    /// <param name="userId">The user id who updated the entity, or null if unauthenticated.</param>
    void SetUpdatedBy(Guid? userId);
}
