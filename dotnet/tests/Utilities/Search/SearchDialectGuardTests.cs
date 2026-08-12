using AQ.Utilities.Search;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.Tests.Search;

[Collection("SearchDialect")]
public class SearchDialectGuardTests : IDisposable
{
    private class Person
    {
        public string? Name { get; set; }
    }

    public SearchDialectGuardTests()
    {
        SearchDialect.Current = null;
    }

    public void Dispose()
    {
        SearchDialect.Current = null;
    }

    private static IQueryable<Person> People() => new List<Person> { new() { Name = "Alice" } }.AsQueryable();

    [Fact]
    public void Fuzzy_WithoutRegisteredDialect_Throws()
    {
        var act = () => People().CreateSearch().Fuzzy(nameof(Person.Name), "Alise").Build();

        act.Should().Throw<NotSupportedException>().WithMessage("*Fuzzy*");
    }

    [Fact]
    public void Phonetic_WithoutRegisteredDialect_Throws()
    {
        var act = () => People().CreateSearch().Search(nameof(Person.Name), "Alise", SearchOperator.Phonetic).Build();

        act.Should().Throw<NotSupportedException>().WithMessage("*Phonetic*");
    }

    [Fact]
    public void FullText_WithoutRegisteredDialect_Throws()
    {
        var act = () => People().CreateSearch().Search(nameof(Person.Name), "Alice", SearchOperator.FullText).Build();

        act.Should().Throw<NotSupportedException>().WithMessage("*FullText*");
    }
}
