using AQ.Utilities.Search;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.Tests.Search;

public class SearchDerivedTypeTests
{
    private class Named
    {
        public string? Name { get; set; }
    }

    private abstract class Link
    {
        [Searchable]
        public string? Rationale { get; set; }
    }

    private class PersonLink : Link
    {
        [Searchable(SubPaths = new[] { "Name" })]
        public Named? Target { get; set; }
    }

    private class PlaceLink : Link
    {
        [Searchable(SubPaths = new[] { "Name" })]
        public Named? Target { get; set; }
    }

    private class OtherBase
    {
        [Searchable]
        public string? Title { get; set; }
    }

    private class OtherDerived : OtherBase
    {
        public string? Extra { get; set; }
    }

    private class HierarchyWithoutAttribute
    {
        public string? Label { get; set; }
    }

    private class HierarchyWithoutAttributeChild : HierarchyWithoutAttribute
    {
        [Searchable]
        public string? Note { get; set; }
    }

    private static IQueryable<Link> Links() => new List<Link>
    {
        new PersonLink { Rationale = "alpha", Target = new Named { Name = "Zed" } },
        new PlaceLink { Rationale = "beta", Target = new Named { Name = "Zed" } },
        new PlaceLink { Rationale = "gamma", Target = new Named { Name = "Paris" } },
        new PersonLink { Rationale = "delta", Target = null }
    }.AsQueryable();

    [Fact]
    public void Extract_KeysDerivedFieldsByTypeName_SoSameMemberOnTwoSubtypesDoesNotCollide()
    {
        var fields = SearchableFieldExtractor.ExtractSearchableFields<Link>();

        fields.Should().ContainKey("Rationale");
        fields.Should().ContainKey("PersonLink:Target.Name");
        fields.Should().ContainKey("PlaceLink:Target.Name");
        fields["PersonLink:Target.Name"].PropertyPath.Should().Be("Target.Name");
        fields["PersonLink:Target.Name"].DerivedType.Should().Be(typeof(PersonLink));
        fields["Rationale"].DerivedType.Should().BeNull();
    }

    [Fact]
    public void Extract_BaseOnlyHierarchy_HasNoDerivedFields()
    {
        var fields = SearchableFieldExtractor.ExtractSearchableFields<OtherBase>();

        fields.Keys.Should().BeEquivalentTo("Title");
    }

    [Fact]
    public void Extract_ReturnsCachedInstance()
    {
        var first = SearchableFieldExtractor.ExtractSearchableFields<Link>();
        var second = SearchableFieldExtractor.ExtractSearchableFields<Link>();

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void ApplyGlobalSearch_DerivedMember_MatchesOnlyThatSubtype()
    {
        var results = Links().ApplyGlobalSearch("zed").ToList();

        results.Should().HaveCount(2);
        results.Should().Contain(l => l is PersonLink && l.Rationale == "alpha");
        results.Should().Contain(l => l is PlaceLink && l.Rationale == "beta");
    }

    [Fact]
    public void ApplyGlobalSearch_DerivedMember_DoesNotThrowOnNullNavigation()
    {
        var act = () => Links().ApplyGlobalSearch("paris").ToList();

        act.Should().NotThrow();
        Links().ApplyGlobalSearch("paris").Single().Rationale.Should().Be("gamma");
    }

    [Fact]
    public void ApplyGlobalSearch_BaseMember_StillMatchesAcrossSubtypes()
    {
        var results = Links().ApplyGlobalSearch("a").ToList();

        results.Should().NotBeEmpty();
        Links().ApplyGlobalSearch("beta").Single().Should().BeOfType<PlaceLink>();
    }

    [Fact]
    public void Fallback_IsSkipped_WhenOnlyADerivedTypeIsMarkedSearchable()
    {
        var fields = SearchableFieldExtractor.ExtractSearchableFields<HierarchyWithoutAttribute>();
        fields.Keys.Should().BeEquivalentTo("HierarchyWithoutAttributeChild:Note");

        var spec = SearchableFieldExtractor.CreateGlobalSearchSpecification<HierarchyWithoutAttribute>("x");
        spec.RootGroup.Conditions.Should().ContainSingle().Which.PropertyPath.Should().Be("Note");
    }

    [Fact]
    public void ApplySearch_Scoring_SkipsDerivedFieldsOfOtherSubtypes()
    {
        var spec = SearchSpecification.Create("zed");

        var results = Links().ApplySearch(spec);

        results.Results.Select(r => r.Entity.Rationale).Should().BeEquivalentTo("alpha", "beta");
    }
}

public class SearchableParentTests
{
    [SearchableParent(SubPaths = new[] { "Name" })]
    private class Node
    {
        [Searchable]
        public string? Name { get; set; }
        public Node? Parent { get; set; }
    }

    private static IQueryable<Node> Nodes()
    {
        var root = new Node { Name = "Engineering" };
        var mid = new Node { Name = "Platform", Parent = root };
        var leaf = new Node { Name = "Core", Parent = mid };
        return new List<Node> { root, mid, leaf }.AsQueryable();
    }

    [Fact]
    public void ApplyGlobalSearch_MatchesOwnAndDirectParentName_NotGrandparent()
    {
        var results = Nodes().ApplyGlobalSearch("engineering").Select(n => n.Name).ToList();

        results.Should().BeEquivalentTo("Engineering", "Platform");
    }

    [Fact]
    public void Extract_AddsParentPaths()
    {
        SearchableFieldExtractor.ExtractSearchableFields<Node>().Keys.Should().BeEquivalentTo("Name", "Parent.Name");
    }
}
