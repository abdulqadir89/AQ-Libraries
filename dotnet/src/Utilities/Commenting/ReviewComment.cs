namespace AQ.Utilities.Commenting;

/// <summary>
/// Base of an app's comment type for reviews: a comment thread can be resolved and reopened.
/// Only the last change is kept: who made it and when.
/// </summary>
public abstract class ReviewComment<TPrincipal> : Comment<TPrincipal> where TPrincipal : class
{
    public bool IsResolved { get; private set; }
    public Guid? ResolutionChangedById { get; private set; }
    public DateTimeOffset? ResolutionChangedAt { get; private set; }

    protected ReviewComment() { }

    // RULE CMT-5
    public void Resolve(Guid actorId) => ChangeResolution(true, actorId);

    // RULE CMT-5
    public void Reopen(Guid actorId) => ChangeResolution(false, actorId);

    private void ChangeResolution(bool resolved, Guid actorId)
    {
        if (ParentId is not null)
        {
            throw new InvalidOperationException("Only a top-level comment can be resolved or reopened.");
        }

        if (IsDeleted)
        {
            throw new InvalidOperationException("A deleted comment cannot be resolved or reopened.");
        }

        if (IsResolved == resolved)
        {
            return;
        }

        // RULE CMT-6
        IsResolved = resolved;
        ResolutionChangedById = actorId;
        ResolutionChangedAt = DateTimeOffset.UtcNow;
    }
}
