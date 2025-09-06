namespace AQ.Abstractions;

public interface IApplicationDbContext
{
    // SaveChanges methods
    int SaveChanges();
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
