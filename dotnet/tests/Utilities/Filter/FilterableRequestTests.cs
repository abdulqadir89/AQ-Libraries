using AQ.Utilities.Filter;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AQ.Utilities.Tests.Filter;

public class FilterableRequestTests
{
    private class Person
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    private class TestRequest : IFilterableRequest
    {
        public string? FilterExpression { get; set; }
    }

    private static IQueryable<Person> People() => new List<Person>
    {
        new() { Id = 1, Name = "Alice", Age = 30, IsActive = true },
        new() { Id = 2, Name = "Bob", Age = 20, IsActive = false },
        new() { Id = 3, Name = "Charlie", Age = 40, IsActive = true }
    }.AsQueryable();

    [Fact]
    public void GetFilterSpecification_NullFilterExpression_ReturnsNull()
    {
        var request = new TestRequest { FilterExpression = null };

        request.GetFilterSpecification().Should().BeNull();
    }

    [Fact]
    public void GetFilterSpecification_EmptyFilterExpression_ReturnsNull()
    {
        var request = new TestRequest { FilterExpression = "   " };

        request.GetFilterSpecification().Should().BeNull();
    }

    [Fact]
    public void GetFilterSpecification_ValidExpression_Parses()
    {
        var request = new TestRequest { FilterExpression = "Name,eq,Alice" };

        request.GetFilterSpecification().Should().NotBeNull();
    }

    [Fact]
    public void ApplyFilters_NullRequest_ReturnsQueryUnchanged()
    {
        var results = People().ApplyFilters(null).ToList();

        results.Should().HaveCount(3);
    }

    [Fact]
    public void ApplyFilters_SimpleExpression_FiltersCorrectly()
    {
        var request = new TestRequest { FilterExpression = "Name,eq,Alice" };

        var results = People().ApplyFilters(request).ToList();

        results.Should().ContainSingle().Which.Name.Should().Be("Alice");
    }

    [Fact]
    public void ApplyFilters_ComplexExpression_FiltersCorrectly()
    {
        var request = new TestRequest
        {
            FilterExpression = "(Name,startswith,C && Age,gt,25) || (IsActive,eq,false)"
        };

        var results = People().ApplyFilters(request).ToList();

        results.Select(p => p.Name).Should().BeEquivalentTo("Charlie", "Bob");
    }

    private class PersonContext(DbContextOptions<PersonContext> options) : DbContext(options)
    {
        public DbSet<Person> People => Set<Person>();
    }

    private static async Task<PersonContext> CreateSeededSqliteContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PersonContext>()
            .UseSqlite(connection)
            .Options;

        var context = new PersonContext(options);
        await context.Database.EnsureCreatedAsync();

        context.People.AddRange(
            new Person { Name = "Alice", Age = 30, IsActive = true },
            new Person { Name = "Bob", Age = 20, IsActive = false },
            new Person { Name = "Charlie", Age = 40, IsActive = true });
        await context.SaveChangesAsync();

        return context;
    }

    [Fact]
    public async Task ApplyFilters_SimpleExpression_TranslatesUnderEfCoreSqlite()
    {
        await using var context = await CreateSeededSqliteContextAsync();
        var request = new TestRequest { FilterExpression = "Name,eq,Alice" };

        var results = await context.People.ApplyFilters(request).ToListAsync();

        results.Should().ContainSingle().Which.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task ApplyFilters_ContainsExpression_TranslatesUnderEfCoreSqlite()
    {
        await using var context = await CreateSeededSqliteContextAsync();
        var request = new TestRequest { FilterExpression = "Name,contains,li" };

        var results = await context.People.ApplyFilters(request).ToListAsync();

        results.Select(p => p.Name).Should().BeEquivalentTo("Alice", "Charlie");
    }

    [Fact]
    public async Task ApplyFilters_ComplexExpression_TranslatesUnderEfCoreSqlite()
    {
        await using var context = await CreateSeededSqliteContextAsync();
        var request = new TestRequest
        {
            FilterExpression = "(Name,startswith,C && Age,gt,25) || (IsActive,eq,false)"
        };

        var results = await context.People.ApplyFilters(request).ToListAsync();

        results.Select(p => p.Name).Should().BeEquivalentTo("Charlie", "Bob");
    }
}
