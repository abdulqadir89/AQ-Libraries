using System.ComponentModel.DataAnnotations.Schema;
using AQ.Utilities.Search;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.Tests.Search;

public class SearchableFieldExtractorTests
{
    private class PlainEntity
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public List<string> Tags { get; set; } = new();
        public string ReadOnlyComputed => Name + Age;

        [NotMapped]
        public string NotMappedField { get; set; } = string.Empty;

        public string DisplayName { get; private set; } = string.Empty;
    }

    private class AttributedEntity
    {
        [Searchable(Weight = 5.0)]
        public string Title { get; set; } = string.Empty;

        public string Untagged { get; set; } = string.Empty;

        [Searchable(SubPaths = new[] { "Value" })]
        public MarkdownContent Content { get; set; } = new();
    }

    private class MarkdownContent
    {
        public string Value { get; set; } = string.Empty;
    }

    [Fact]
    public void GetDefaultSearchableFields_IncludesStringProperties()
    {
        var fields = SearchableFieldExtractor.GetDefaultSearchableFields<PlainEntity>();

        fields.Should().ContainKey(nameof(PlainEntity.Name));
    }

    [Fact]
    public void GetDefaultSearchableFields_IncludesPrimitiveProperties()
    {
        var fields = SearchableFieldExtractor.GetDefaultSearchableFields<PlainEntity>();

        fields.Should().ContainKey(nameof(PlainEntity.Age));
    }

    [Fact]
    public void GetDefaultSearchableFields_ExcludesCollectionProperties()
    {
        var fields = SearchableFieldExtractor.GetDefaultSearchableFields<PlainEntity>();

        fields.Should().NotContainKey(nameof(PlainEntity.Tags));
    }

    [Fact]
    public void GetDefaultSearchableFields_ExcludesGetOnlyProperties()
    {
        var fields = SearchableFieldExtractor.GetDefaultSearchableFields<PlainEntity>();

        fields.Should().NotContainKey(nameof(PlainEntity.ReadOnlyComputed));
    }

    [Fact]
    public void GetDefaultSearchableFields_IncludesPrivateSetterProperties()
    {
        var fields = SearchableFieldExtractor.GetDefaultSearchableFields<PlainEntity>();

        fields.Should().ContainKey(nameof(PlainEntity.DisplayName));
    }

    [Fact]
    public void GetDefaultSearchableFields_ExcludesNotMappedProperties()
    {
        var fields = SearchableFieldExtractor.GetDefaultSearchableFields<PlainEntity>();

        fields.Should().NotContainKey(nameof(PlainEntity.NotMappedField));
    }

    [Fact]
    public void ExtractSearchableFields_ReturnsWeightFromAttribute()
    {
        var fields = SearchableFieldExtractor.ExtractSearchableFields<AttributedEntity>();

        fields[nameof(AttributedEntity.Title)].Weight.Should().Be(5.0);
    }

    [Fact]
    public void ExtractSearchableFields_IgnoresPropertiesWithoutAttribute()
    {
        var fields = SearchableFieldExtractor.ExtractSearchableFields<AttributedEntity>();

        fields.Should().NotContainKey(nameof(AttributedEntity.Untagged));
    }

    [Fact]
    public void ExtractSearchableFields_SubPaths_RegistersLeafField()
    {
        var fields = SearchableFieldExtractor.ExtractSearchableFields<AttributedEntity>();

        fields.Should().ContainKey($"{nameof(AttributedEntity.Content)}.Value");
    }
}
