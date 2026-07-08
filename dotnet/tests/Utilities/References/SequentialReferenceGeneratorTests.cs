using AQ.Utilities.References;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.References.Tests;

public class SequentialReferenceGeneratorTests
{
    private sealed class FakeSequentialReferenceGenerator(int nextValue) : SequentialReferenceGenerator
    {
        protected override int GetNextSequenceValue(ReferenceGenerationContext context) => nextValue;
    }

    [Fact]
    public void Generate_WithPrefix_ReturnsPrefixedFourDigitPaddedSequence()
    {
        // Arrange
        var generator = new FakeSequentialReferenceGenerator(42);
        var context = new ReferenceGenerationContext(Prefix: "ACT");

        // Act
        var result = generator.Generate(context);

        // Assert
        result.Should().Be("ACT-0042");
    }

    [Fact]
    public void Generate_WithoutPrefix_ReturnsFourDigitPaddedSequence()
    {
        // Arrange
        var generator = new FakeSequentialReferenceGenerator(7);
        var context = new ReferenceGenerationContext();

        // Act
        var result = generator.Generate(context);

        // Assert
        result.Should().Be("0007");
    }
}
