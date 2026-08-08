using AQ.Utilities.Search;
using AQ.Utilities.Search.SqlServer;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AQ.Utilities.Tests.Search;

[Collection("SearchDialect")]
public class SqlServerDialectTests : IDisposable
{
    private class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class TestDbContext : DbContext
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=.;Database=SearchDialectTests;Trusted_Connection=True;TrustServerCertificate=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddSqlServerSearchFunctions();
        }
    }

    public SqlServerDialectTests()
    {
        SqlServerSearchRegistration.Register();
    }

    public void Dispose()
    {
        SearchDialect.Current = null;
    }

    [Fact]
    public void Fuzzy_TranslatesToDifference()
    {
        using var context = new TestDbContext();

        var spec = SearchSpecification.CreateForField(nameof(TestEntity.Name), "Jhon", SearchOperator.Fuzzy);
        var query = context.Entities.ApplySearchAsQueryable(spec);

        query.ToQueryString().Should().Contain("DIFFERENCE(");
    }

    [Fact]
    public void Phonetic_TranslatesToSoundex()
    {
        using var context = new TestDbContext();

        var spec = SearchSpecification.CreateForField(nameof(TestEntity.Name), "Jhon", SearchOperator.Phonetic);
        var query = context.Entities.ApplySearchAsQueryable(spec);

        query.ToQueryString().Should().Contain("SOUNDEX(");
    }

    [Fact]
    public void Contains_StillTranslatesToLike()
    {
        using var context = new TestDbContext();

        var query = context.Entities.ApplyGlobalSearch("test");

        query.ToQueryString().Should().Contain("LIKE");
    }
}
