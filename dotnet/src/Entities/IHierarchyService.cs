namespace AQ.Abstractions;

public interface IHierarchyService<T> where T : IHierarchicalEntity
{
    Task UpdateClosureOnParentChangeAsync(T entity, Guid? newParentId);

    Task<bool> CircularReferenceExists(T entity, Guid? newParentId);

    Task<bool> IsDescendantAsync(T entity, Guid ancestorId);
    Task<bool> IsAncestorAsync(T entity, Guid descendantId);
}
