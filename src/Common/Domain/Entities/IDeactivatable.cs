namespace AQ.Common.Domain.Entities;

/// <summary>
/// Interface for entities that can be deactivated/activated.
/// Provides soft delete functionality with audit trail support.
/// </summary>
public interface IDeactivatable
{
    /// <summary>
    /// Indicates whether the entity is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// The user who deactivated this entity (null if currently active).
    /// </summary>
    Guid? DeactivatedById { get; }

    /// <summary>
    /// When the entity was deactivated (null if currently active).
    /// </summary>
    DateTime? DeactivatedAt { get; }

    /// <summary>
    /// Optional reason for deactivation.
    /// </summary>
    string? DeactivationReason { get; }

    /// <summary>
    /// Deactivates the entity with audit information.
    /// </summary>
    /// <param name="userId">The ID of the user performing the deactivation</param>
    /// <param name="reason">Optional reason for deactivation</param>
    void Deactivate(Guid userId, string? reason = null);

    /// <summary>
    /// Reactivates the entity, clearing deactivation audit information.
    /// </summary>
    void Reactivate();
}
