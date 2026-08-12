using AQ.Utilities.References;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.References.Tests;

public class WordSlugReferenceGeneratorTests
{
    private readonly WordSlugReferenceGenerator _generator = new();

    [Fact]
    public void Generate_WithPrefix_ReturnsPrefixedAdjectiveNounNumberSlug()
    {
        // Arrange
        var context = new ReferenceGenerationContext(Prefix: "ACT");

        // Act
        var result = _generator.Generate(context);

        // Assert
        result.Should().MatchRegex("^ACT-[a-z]+-[a-z]+-[0-9]{2}$");
    }

    [Fact]
    public void Generate_WithoutPrefix_ReturnsUnprefixedAdjectiveNounNumberSlug()
    {
        // Arrange
        var context = new ReferenceGenerationContext();

        // Act
        var result = _generator.Generate(context);

        // Assert
        result.Should().MatchRegex("^[a-z]+-[a-z]+-[0-9]{2}$");
    }
}
