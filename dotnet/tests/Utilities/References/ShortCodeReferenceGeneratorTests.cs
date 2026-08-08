using AQ.Utilities.References;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.References.Tests;

public class ShortCodeReferenceGeneratorTests
{
    private readonly ShortCodeReferenceGenerator _generator = new();

    [Fact]
    public void Generate_WithPrefix_ReturnsPrefixedSixCharCode()
    {
        // Arrange
        var context = new ReferenceGenerationContext(Prefix: "ACT", Length: 6);

        // Act
        var result = _generator.Generate(context);

        // Assert
        result.Should().MatchRegex("^ACT-[0-9A-HJKMNP-TV-Z]{6}$");
    }

    [Fact]
    public void Generate_WithoutPrefix_ReturnsUnprefixedSixCharCode()
    {
        // Arrange
        var context = new ReferenceGenerationContext(Length: 6);

        // Act
        var result = _generator.Generate(context);

        // Assert
        result.Should().MatchRegex("^[0-9A-HJKMNP-TV-Z]{6}$");
    }

    [Fact]
    public void Generate_CalledManyTimes_ProducesUniqueValues()
    {
        // Arrange
        var context = new ReferenceGenerationContext(Prefix: "ACT", Length: 6);

        // Act
        var results = Enumerable.Range(0, 1000).Select(_ => _generator.Generate(context)).ToHashSet();

        // Assert
        results.Should().HaveCount(1000);
    }

    [Fact]
    public void Generate_WithoutLength_ThrowsArgumentException()
    {
        // Arrange
        var context = new ReferenceGenerationContext(Prefix: "ACT");

        // Act
        var act = () => _generator.Generate(context);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
