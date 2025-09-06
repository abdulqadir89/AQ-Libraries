namespace AQ.Domain.Entities;

public interface IHasAttachments
{
    public string EntityType => GetType().Name;
}
