namespace AQ.EntityFramework.SaveChanges;

public interface ICurrentUserProvider
{
    Guid? CurrentUserId { get; }
}
