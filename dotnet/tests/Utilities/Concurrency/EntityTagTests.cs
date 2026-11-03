using Xunit;
using AQ.Utilities.Concurrency;
using FluentAssertions;

namespace AQ.Utilities.Tests.Concurrency;

public class EntityTagTests
{
    [Fact]
    public void Format_QuotesTheRevision() => EntityTag.Format(7).Should().Be("\"7\"");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyValue_IsMissing(string? value) =>
        EntityTag.Parse(value).Kind.Should().Be(IfMatchKind.Missing);

    [Fact]
    public void Parse_Star_IsAny() => EntityTag.Parse("*").Kind.Should().Be(IfMatchKind.Any);

    [Fact]
    public void Parse_QuotedRevision_ReturnsTheRevision() =>
        EntityTag.Parse(" \"12\" ").Should().Be(new IfMatch(IfMatchKind.Revision, 12));

    [Theory]
    [InlineData("12")]
    [InlineData("W/\"12\"")]
    [InlineData("\"abc\"")]
    [InlineData("\"-1\"")]
    [InlineData("\"1\",\"2\"")]
    [InlineData("\"\"")]
    public void Parse_AnythingElse_IsInvalid(string value) =>
        EntityTag.Parse(value).Kind.Should().Be(IfMatchKind.Invalid);
}
