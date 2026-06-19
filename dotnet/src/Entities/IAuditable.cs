namespace AQ.Abstractions;

public interface ITimestamped
{
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset? UpdatedAt { get; }
    int Revision { get; }
}

public interface IAuditable : ITimestamped
{
    Guid? CreatedById { get; }
    Guid? UpdatedById { get; }
    void SetCreatedBy(Guid? userId);
    void SetUpdatedBy(Guid? userId);
}

public interface IAuditable<TUser> : IAuditable
{
    TUser? CreatedBy { get; }
    TUser? UpdatedBy { get; }
}
