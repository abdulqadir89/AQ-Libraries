namespace AQ.Entities;

public interface IUser<TUserId>
{
    TUserId Id { get; }
}
