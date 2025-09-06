namespace AQ.Domain.Enums;

/// <summary>
/// Represents the type of audit action performed on an entity
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// Entity was created
    /// </summary>
    Created = 1,

    /// <summary>
    /// Entity was updated
    /// </summary>
    Updated = 2,

    /// <summary>
    /// Entity was deleted (hard delete)
    /// </summary>
    Deleted = 3,

    /// <summary>
    /// Entity was deactivated (soft delete)
    /// </summary>
    Deactivated = 4,

    /// <summary>
    /// Entity was reactivated
    /// </summary>
    Reactivated = 5
}
