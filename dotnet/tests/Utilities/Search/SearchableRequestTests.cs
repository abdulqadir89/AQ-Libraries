using AQ.Utilities.Search;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.Tests.Search;

public class SearchableRequestTests
{
    private class Person
    {
        public string? Name { get; set; }
    }

    private static IQueryable<Person> People() => new List<Person>
    {
        new() { Name = "Alice" },
        new() { Name = "Alicia" },
        new() { Name = "Bob" }
    }.AsQueryable();

    [Fact]
    public void ApplySearch_NullRequest_ReturnsAllUnscored()
    {
        var results = People().ApplySearch((ISearchableRequest?)null);

        results.Results.Should().HaveCount(3);
        results.Results.Should().OnlyContain(r => r.Score == 1.0);
    }

    [Fact]
    public void ApplySearch_EmptySearchTerm_ReturnsAllUnscored()
    {
        var request = new SearchableRequest { SearchTerm = "" };

        var results = People().ApplySearch(request);

        results.Results.Should().HaveCount(3);
    }

    [Fact]
    public void ApplySearch_MaxResults_CapsResultCount()
    {
        var request = new SearchableRequest { SearchTerm = "Ali", MaxResults = 1 };

        var results = People().ApplySearch(request);

        results.Results.Should().HaveCount(1);
    }

    [Fact]
    public void ApplySearch_MinScore_FiltersLowScoringResults()
    {
        var request = new SearchableRequest { SearchTerm = "Alice", MinScore = 0.99 };

        var results = People().ApplySearch(request);

        results.Results.Should().OnlyContain(r => r.Entity.Name == "Alice");
    }
}
