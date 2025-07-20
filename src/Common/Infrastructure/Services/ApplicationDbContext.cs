using AQ.Common.Application.Services;
using AQ.Common.Application.Specifications.Interfaces;
using AQ.Common.Domain.Entities;
using AQ.Common.Domain.Events;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AQ.Common.Infrastructure.Services;

public abstract class ApplicationDbContext<TContext>(
    DbContextOptions<TContext> options,
    ICurrentUserService currentUserService,
    IDomainEventDispatcher domainEventDispatcher) : DbContext(options), IApplicationDbContext where TContext : DbContext
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        await DispatchDomainEventsAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditableEntities();
        DispatchDomainEventsAsync(CancellationToken.None).GetAwaiter().GetResult();
        return base.SaveChanges();
    }

    protected void UpdateAuditableEntities()
    {
        var currentUserId = currentUserService.GetCurrentUserId();

        // Only update audit fields if we have a current user
        if (!currentUserId.HasValue)
            return;

        var auditableEntries = ChangeTracker.Entries<AuditableEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in auditableEntries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreated(currentUserId.Value);
                    break;

                case EntityState.Modified:
                    entry.Entity.SetUpdated(currentUserId.Value);
                    break;
            }
        }
    }

    protected async Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default)
    {
        if (domainEventDispatcher == null)
            return;

        // Get all entities with domain events
        var entitiesWithDomainEvents = ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        if (!entitiesWithDomainEvents.Any())
            return;

        // Dispatch domain events
        await domainEventDispatcher.DispatchEventsAsync(entitiesWithDomainEvents, cancellationToken);
    }

    protected static void ConfigureConcurrencyTokens(ModelBuilder modelBuilder)
    {
        // Configure RowVersion as concurrency token for all entities inheriting from Entity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<byte[]>(nameof(Entity.RowVersion))
                    .IsRowVersion()
                    .HasConversion<byte[]>()
                    .ValueGeneratedOnAddOrUpdate();
            }
        }
    }

    // Implementation of IApplicationDbContext interface methods
    public new IQueryable<T> Set<T>() where T : class
    {
        return base.Set<T>();
    }

    public async Task<T?> GetByIdAsync<T>(object id, IIncludeSpecification<T>? includeSpec = null, CancellationToken cancellationToken = default) where T : class
    {
        var query = Set<T>().AsQueryable();

        if (includeSpec != null)
        {
            query = ApplyIncludes(query, includeSpec);
        }

        return await query.FirstOrDefaultAsync(e => EF.Property<object>(e, "Id").Equals(id), cancellationToken);
    }

    public async Task<T?> GetAsync<T>(IQuerySpecification<T> specification, CancellationToken cancellationToken = default) where T : class
    {
        var query = ApplySpecification(Set<T>(), specification);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<T>> GetListAsync<T>(IQuerySpecification<T> specification, CancellationToken cancellationToken = default) where T : class
    {
        var query = ApplySpecification(Set<T>(), specification);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync<T>(IQuerySpecification<T> specification, CancellationToken cancellationToken = default) where T : class
    {
        var query = ApplySpecification(Set<T>(), specification, includeOrderingAndPaging: false);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync<T>(IQuerySpecification<T> specification, CancellationToken cancellationToken = default) where T : class
    {
        var query = ApplySpecification(Set<T>(), specification, includeOrderingAndPaging: false);
        return await query.AnyAsync(cancellationToken);
    }

    private IQueryable<T> ApplySpecification<T>(IQueryable<T> query, IQuerySpecification<T> specification, bool includeOrderingAndPaging = true) where T : class
    {
        // Apply includes
        query = ApplyIncludes(query, specification);

        // Apply criteria
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply ordering and paging only if requested (not for Count/Any operations)
        if (!includeOrderingAndPaging)
            return query;

        // Apply ordering
        foreach (var orderBy in specification.OrderBy)
        {
            query = query.OrderBy(orderBy);
        }

        foreach (var orderByDesc in specification.OrderByDescending)
        {
            query = query.OrderByDescending(orderByDesc);
        }

        // Apply paging
        if (specification.IsPagingEnabled)
        {
            if (specification.Skip.HasValue)
            {
                query = query.Skip(specification.Skip.Value);
            }

            if (specification.Take.HasValue)
            {
                query = query.Take(specification.Take.Value);
            }
        }

        return query;
    }

    private IQueryable<T> ApplyIncludes<T>(IQueryable<T> query, IIncludeSpecification<T> includeSpec) where T : class
    {
        // Apply string-based includes
        foreach (var include in includeSpec.IncludeStrings)
        {
            query = query.Include(include);
        }

        // Apply expression-based includes
        foreach (var includeExpression in includeSpec.IncludeExpressions)
        {
            if (includeExpression.Include is Expression<Func<T, object>> typedInclude)
            {
                query = query.Include(typedInclude);

                // Apply ThenInclude if present
                if (includeExpression.ThenInclude != null)
                {
                    // This is more complex as it requires proper type handling
                    // For now, we'll convert to string and use string-based include
                    var includeString = GetIncludeString(typedInclude);
                    var thenIncludeString = GetIncludeString(includeExpression.ThenInclude);
                    query = query.Include($"{includeString}.{thenIncludeString}");
                }
            }
        }

        return query;
    }

    private string GetIncludeString<T>(Expression<Func<T, object>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression innerMemberExpression)
        {
            return innerMemberExpression.Member.Name;
        }

        throw new ArgumentException("Invalid include expression", nameof(expression));
    }

    private string GetIncludeString(LambdaExpression expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression innerMemberExpression)
        {
            return innerMemberExpression.Member.Name;
        }

        throw new ArgumentException("Invalid include expression");
    }


    public new void Add<T>(T entity) where T : class => base.Add(entity);
    public new void Update<T>(T entity) where T : class => base.Update(entity);
    public new void Remove<T>(T entity) where T : class => base.Remove(entity);
}
