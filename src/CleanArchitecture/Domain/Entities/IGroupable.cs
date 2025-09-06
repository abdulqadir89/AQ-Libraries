namespace AQ.Domain.Entities;

public interface IGroupable<T> where T : IEntity
{
    Guid GroupId { get; }
    IEnumerable<T> GetGroupMembers();
}
