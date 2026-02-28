namespace AQ.Abstractions;

public interface IUser<TUserId>
{
    TUserId Id { get; }
}
