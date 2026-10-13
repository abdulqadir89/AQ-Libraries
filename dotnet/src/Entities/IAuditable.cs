namespace AQ.Entities;

public interface ITimestamped
{
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset? UpdatedAt { get; }
    int Revision { get; }
}

public interface IAuditable : ITimestamped
{
    Guid CreatedById { get; }
    Guid UpdatedById { get; }
    void SetCreatedBy(Guid principalId);
    void SetUpdatedBy(Guid principalId);
}

public interface IAuditable<TPrincipal> : IAuditable
{
    TPrincipal? CreatedBy { get; }
    TPrincipal? UpdatedBy { get; }
}
