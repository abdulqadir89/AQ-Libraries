namespace AQ.Abstractions;

public interface IEntity
{
    public Guid Id { get; }
}

public interface IEntity<TEntityId> where TEntityId : IEquatable<TEntityId>
{
    public TEntityId Id { get; }
}