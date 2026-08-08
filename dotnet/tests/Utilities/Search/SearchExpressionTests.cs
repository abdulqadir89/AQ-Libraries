using AQ.Utilities.Search;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.Tests.Search;

public class SearchExpressionTests
{
    private class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public Guid Id { get; set; }
        public Company? Employer { get; set; }
    }

    private class Company
    {
        public string? Name { get; set; }
    }

    private static IQueryable<Person> People() => new List<Person>
    {
        new() { Name = "Alice Smith", Age = 30, Id = Guid.Parse("11111111-1111-1111-1111-111111111111") },
        new() { Name = "bob jones", Age = 40, Id = Guid.Parse("22222222-2222-2222-2222-222222222222") },
        new() { Name = null, Age = 50, Id = Guid.Empty },
        new() { Name = "Alice Cooper", Age = 60, Id = Guid.Empty, Employer = new Company { Name = "Acme" } }
    }.AsQueryable();

    [Fact]
    public void ApplyGlobalSearch_Contains_MatchesCaseInsensitive()
    {
        var results = People().ApplyGlobalSearch("ALICE").ToList();

        results.Should().HaveCount(2);
    }

    [Fact]
    public void ApplyGlobalSearch_NullSearchTerm_ReturnsUnchangedQuery()
    {
        var query = People();
        var results = query.ApplyGlobalSearch(null).ToList();

        results.Should().HaveCount(4);
    }

    [Fact]
    public void ApplyGlobalSearch_DoesNotThrow_OnNullStringProperty()
    {
        var act = () => People().ApplyGlobalSearch("smith").ToList();

        act.Should().NotThrow();
    }

    [Fact]
    public void SearchInProperties_StartsWith_MatchesExpected()
    {
        var results = People()
            .SearchInProperties("bob", p => p.Name)
            .ToList();

        results.Should().ContainSingle(p => p.Name == "bob jones");
    }

    [Fact]
    public void SearchInPropertiesWithNavigation_MatchesNestedProperty()
    {
        var results = People()
            .SearchInPropertiesWithNavigation("acme", p => p.Employer!.Name)
            .ToList();

        results.Should().ContainSingle(p => p.Employer != null);
    }

    [Fact]
    public void CreateSearch_ExactOnInt_MatchesOnlyEqualValue()
    {
        var results = People()
            .CreateSearch()
            .Exact(nameof(Person.Age), "30")
            .Build();

        results.Results.Should().ContainSingle();
        results.Results[0].Entity.Age.Should().Be(30);
    }

    [Fact]
    public void CreateSearch_ExactOnGuid_MatchesOnlyEqualValue()
    {
        var results = People()
            .CreateSearch()
            .Exact(nameof(Person.Id), "11111111-1111-1111-1111-111111111111")
            .Build();

        results.Results.Should().ContainSingle();
    }

    [Fact]
    public void CreateSearch_UnparseableNumericTerm_MatchesNothing()
    {
        var results = People()
            .CreateSearch()
            .Exact(nameof(Person.Age), "not-a-number")
            .Build();

        results.Results.Should().BeEmpty();
    }

    [Fact]
    public void CreateSearch_UnknownPropertyPath_SkipsConditionButAppliesOthers()
    {
        var results = People()
            .CreateSearch()
            .All()
            .Contains("DoesNotExist", "whatever")
            .Contains(nameof(Person.Name), "alice")
            .Build();

        // Unknown property yields a null expression that's skipped; the remaining
        // Contains("alice") condition still applies.
        results.Results.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateSearch_AllMatchType_RequiresAllConditions()
    {
        var results = People()
            .CreateSearch()
            .All()
            .Contains(nameof(Person.Name), "alice")
            .Exact(nameof(Person.Age), "30")
            .Build();

        results.Results.Should().ContainSingle();
    }

    [Fact]
    public void CreateSearch_AnyMatchType_MatchesEitherCondition()
    {
        var results = People()
            .CreateSearch()
            .Any()
            .Exact(nameof(Person.Age), "30")
            .Exact(nameof(Person.Age), "40")
            .Build();

        results.Results.Should().HaveCount(2);
    }

    [Fact]
    public void CreateSearch_NotNegatesGroup()
    {
        var spec = People()
            .CreateSearch()
            .Contains(nameof(Person.Name), "alice")
            .Not()
            .GetSpecification();

        var filtered = People().ApplySearchAsQueryable(spec).ToList();

        filtered.Should().NotContain(p => p.Name != null && p.Name.Contains("Alice", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateSearch_NestedGroup_CombinesWithParent()
    {
        var spec = People()
            .CreateSearch()
            .All()
            .Contains(nameof(Person.Name), "alice")
            .BeginGroup()
            .Exact(nameof(Person.Age), "30")
            .EndGroup()
            .GetSpecification();

        var filtered = People().ApplySearchAsQueryable(spec).ToList();

        filtered.Should().ContainSingle();
    }

    [Fact]
    public void ApplySearchAsQueryable_NullSpecification_ReturnsUnchangedQuery()
    {
        var filtered = People().ApplySearchAsQueryable(null).ToList();

        filtered.Should().HaveCount(4);
    }
}
