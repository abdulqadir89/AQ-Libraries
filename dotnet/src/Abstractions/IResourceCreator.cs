namespace AQ.Abstractions;

public interface IResourceCreator
{
    public Guid? CreatedById { get; }
    public DateTimeOffset CreatedAt { get; }
}