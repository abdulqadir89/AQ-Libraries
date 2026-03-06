namespace AQ.Abstractions;

public interface IEntity
{
    public Guid Id { get; }

    /// <summary>
    /// Gets the entity type name used for filtering and identification.
    /// Should return a unique, stable identifier for the entity type (e.g., "Asset", "Project", "Training").
    /// </summary>
    string GetEntityTypeName() => GetType().Name;
}

public interface IEntity<TEntityId> where TEntityId : IEquatable<TEntityId>
{
    public TEntityId Id { get; }

    /// <summary>
    /// Gets the entity type name used for filtering and identification.
    /// Should return a unique, stable identifier for the entity type (e.g., "Asset", "Project", "Training").
    /// </summary>
    string GetEntityTypeName() => GetType().Name;
}
