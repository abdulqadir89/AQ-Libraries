namespace AQ.Domain.Entities;

public interface IUser<TUserId>
{
    TUserId Id { get; }
}
