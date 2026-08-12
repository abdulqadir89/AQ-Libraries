using AQ.Utilities.Search;
using AQ.Utilities.Search.PostgreSql;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AQ.Utilities.Tests.Search;

[Collection("SearchDialect")]
public class PostgreSqlDialectTests : IDisposable
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
            optionsBuilder.UseNpgsql("Host=localhost;Database=search_dialect_tests;Username=test;Password=test");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgreSqlSearchExtensions();
        }
    }

    public PostgreSqlDialectTests()
    {
        PostgreSqlSearchRegistration.Register();
    }

    public void Dispose()
    {
        SearchDialect.Current = null;
    }

    [Fact]
    public void Fuzzy_TranslatesToTrigramSimilarity()
    {
        using var context = new TestDbContext();

        var spec = SearchSpecification.CreateForField(nameof(TestEntity.Name), "Jhon", SearchOperator.Fuzzy);
        var query = context.Entities.ApplySearchAsQueryable(spec);

        query.ToQueryString().Should().Contain("similarity(");
    }

    [Fact]
    public void Phonetic_TranslatesToSoundex()
    {
        using var context = new TestDbContext();

        var spec = SearchSpecification.CreateForField(nameof(TestEntity.Name), "Jhon", SearchOperator.Phonetic);
        var query = context.Entities.ApplySearchAsQueryable(spec);

        query.ToQueryString().Should().Contain("soundex(");
    }

    [Fact]
    public void FullText_TranslatesToTsVector()
    {
        using var context = new TestDbContext();

        var spec = SearchSpecification.CreateForField(nameof(TestEntity.Name), "hello world", SearchOperator.FullText);
        var query = context.Entities.ApplySearchAsQueryable(spec);

        query.ToQueryString().Should().Contain("to_tsvector");
    }

    [Fact]
    public void Contains_StillTranslatesToLike()
    {
        using var context = new TestDbContext();

        var query = context.Entities.ApplyGlobalSearch("test");

        query.ToQueryString().Should().Contain("LIKE");
    }
}
