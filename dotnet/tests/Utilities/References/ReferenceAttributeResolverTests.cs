using AQ.Utilities.References;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.References.Tests;

public class ReferenceAttributeResolverTests
{
    private class SampleEntity
    {
        [GeneratedReference(typeof(ShortCodeReferenceGenerator), 6, Prefix = "ACT")]
        public string? Reference { get; set; }

        public string? Undecorated { get; set; }
    }

    [Fact]
    public void ResolveContext_DecoratedProperty_ReturnsContextFromAttribute()
    {
        // Act
        var context = ReferenceAttributeResolver.ResolveContext<SampleEntity>(e => e.Reference);

        // Assert
        context.Prefix.Should().Be("ACT");
        context.Length.Should().Be(6);
    }

    [Fact]
    public void ResolveContext_UndecoratedProperty_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => ReferenceAttributeResolver.ResolveContext<SampleEntity>(e => e.Undecorated);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Generate_DecoratedProperty_UsesDeclaredGeneratorAndParams()
    {
        // Act
        var result = ReferenceAttributeResolver.Generate<SampleEntity>(e => e.Reference);

        // Assert
        result.Should().MatchRegex("^ACT-[0-9A-HJKMNP-TV-Z]{6}$");
    }

    [Fact]
    public void Generate_UndecoratedProperty_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => ReferenceAttributeResolver.Generate<SampleEntity>(e => e.Undecorated);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ApplyGeneratedReferences_BlankDecoratedProperty_FillsInGeneratedValue()
    {
        // Arrange
        var entity = new SampleEntity { Reference = null };

        // Act
        ReferenceAttributeResolver.ApplyGeneratedReferences(entity);

        // Assert
        entity.Reference.Should().MatchRegex("^ACT-[0-9A-HJKMNP-TV-Z]{6}$");
    }

    [Fact]
    public void ApplyGeneratedReferences_AlreadySetDecoratedProperty_LeavesValueUnchanged()
    {
        // Arrange
        var entity = new SampleEntity { Reference = "MANUAL-REF" };

        // Act
        ReferenceAttributeResolver.ApplyGeneratedReferences(entity);

        // Assert
        entity.Reference.Should().Be("MANUAL-REF");
    }

    [Fact]
    public void ApplyGeneratedReferences_UndecoratedProperty_LeavesNullUnchanged()
    {
        // Arrange
        var entity = new SampleEntity { Undecorated = null };

        // Act
        ReferenceAttributeResolver.ApplyGeneratedReferences(entity);

        // Assert
        entity.Undecorated.Should().BeNull();
    }
}
