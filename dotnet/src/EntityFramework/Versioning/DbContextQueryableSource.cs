using AQ.Utilities.Versioning;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Versioning;

public sealed class DbContextQueryableSource(DbContext db) : IQueryableSource
{
    public IQueryable<T> Set<T>() where T : class => db.Set<T>();
}
