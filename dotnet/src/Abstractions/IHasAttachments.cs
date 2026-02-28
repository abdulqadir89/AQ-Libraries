namespace AQ.Abstractions;

public interface IHasAttachments
{
    public string EntityType => GetType().Name;
}
