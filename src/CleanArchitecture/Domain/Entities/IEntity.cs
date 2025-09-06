namespace AQ.Domain.Entities;

public interface IEntity
{
    public Guid Id { get; }
}

public interface IEntity<TEntityId> where TEntityId : IEquatable<TEntityId>
{
    public TEntityId Id { get; }
}