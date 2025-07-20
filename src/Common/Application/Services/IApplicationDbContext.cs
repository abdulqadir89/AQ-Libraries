using AQ.Common.Application.Specifications.Interfaces;

namespace AQ.Common.Application.Services;

public interface IApplicationDbContext
{
    // SaveChanges methods
    int SaveChanges();
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Query methods with specifications
    Task<T?> GetByIdAsync<T>(object id, IIncludeSpecification<T>? includeSpec = null, CancellationToken cancellationToken = default) where T : class;
    Task<T?> GetAsync<T>(IQuerySpecification<T> specification, CancellationToken cancellationToken = default) where T : class;
    Task<List<T>> GetListAsync<T>(IQuerySpecification<T> specification, CancellationToken cancellationToken = default) where T : class;
    Task<int> CountAsync<T>(IQuerySpecification<T> specification, CancellationToken cancellationToken = default) where T : class;
    Task<bool> AnyAsync<T>(IQuerySpecification<T> specification, CancellationToken cancellationToken = default) where T : class;

    // DbSet access for basic operations
    IQueryable<T> Set<T>() where T : class;

    // Generic entity operations
    void Add<T>(T entity) where T : class;
    void Update<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
}
