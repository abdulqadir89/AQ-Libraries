namespace AQ.Common.Domain.Entities;

public abstract class AuditableEntity : Entity, IDeactivatable
{
    public Guid CreatedById { get; private set; } = default!;
    public ApplicationUser CreatedBy { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public Guid? UpdatedById { get; private set; } = null;
    public ApplicationUser? UpdatedBy { get; private set; } = null;
    public DateTime? UpdatedAt { get; private set; } = null;

    public bool IsActive { get; private set; } = true;
    public Guid? DeactivatedById { get; private set; }
    public ApplicationUser? DeactivatedBy { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }
    public string? DeactivationReason { get; private set; }

    public void SetCreated(Guid userId)
    {
        CreatedById = userId;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetUpdated(Guid userId)
    {
        UpdatedById = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate(Guid userId, string? reason = null)
    {
        if (!IsActive)
            throw new InvalidOperationException("Entity is already deactivated.");

        IsActive = false;
        DeactivatedById = userId;
        DeactivatedAt = DateTime.UtcNow;
        DeactivationReason = reason;
    }

    public void Reactivate()
    {
        if (IsActive)
            throw new InvalidOperationException("Entity is already active.");

        IsActive = true;
        DeactivatedById = null;
        DeactivatedAt = null;
        DeactivationReason = null;
    }
}

