namespace AQ.Utilities.Versioning;

public interface IQueryableSource
{
    IQueryable<T> Set<T>() where T : class;
}
