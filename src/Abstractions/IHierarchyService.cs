namespace AQ.Abstractions;

public interface IHierarchyService<T> where T : IHierarchicalEntity
{
    Task UpdateClosureOnParentChangeAsync(T entity, Guid? newParentId);

    Task<bool> CircularReferenceExists(T entity, Guid? newParentId);

}
