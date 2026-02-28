namespace AQ.Abstractions;

/// <summary>
/// Represents an entity that has an owner.
/// </summary>
public interface IHasOwner
{
    /// <summary>
    /// Gets the ID of the owner of this resource.
    /// </summary>
    public Guid OwnerId { get; }
}
