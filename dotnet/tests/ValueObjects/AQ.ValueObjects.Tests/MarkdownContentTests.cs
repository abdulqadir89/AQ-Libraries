using AQ.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AQ.ValueObjects.Tests;

public class MarkdownContentTests
{
    [Fact]
    public void Create_ConvertsHeadingsAndBold()
    {
        var content = MarkdownContent.Create("# Title\n\n**bold** text");

        content.Html.Should().Contain("<h1>Title</h1>");
        content.Html.Should().Contain("<strong>bold</strong>");
    }

    [Fact]
    public void Create_ConvertsListsAndLinks()
    {
        var content = MarkdownContent.Create("- item one\n- item two\n\n[link](https://example.com)");

        content.Html.Should().Contain("<ul>");
        content.Html.Should().Contain("<li>item one</li>");
        content.Html.Should().Contain("href=\"https://example.com\"");
    }

    [Fact]
    public void Create_ConvertsTables()
    {
        var content = MarkdownContent.Create("| A | B |\n|---|---|\n| 1 | 2 |");

        content.Html.Should().Contain("<table>");
    }

    [Fact]
    public void Create_EscapesScriptTagsEmbeddedInMarkdown()
    {
        var content = MarkdownContent.Create("plain text <script>alert('xss')</script>");

        content.Html.Should().NotContain("<script>");
    }

    [Fact]
    public void Create_EscapesRawHtmlOnClickAttribute()
    {
        var content = MarkdownContent.Create("<p onclick=\"alert('xss')\">hello</p>");

        content.Html.Should().NotContain("<p onclick=");
    }

    [Fact]
    public void Create_EscapesRawHtmlJavascriptScheme()
    {
        var content = MarkdownContent.Create("<a href=\"javascript:alert('xss')\">click</a>");

        content.Html.Should().NotContain("<a href=\"javascript:");
    }

    [Fact]
    public void Create_EmptyInput_ProducesEmptyOutput()
    {
        var content = MarkdownContent.Create("");

        content.Value.Should().BeEmpty();
        content.Html.Should().BeEmpty();
    }

    [Fact]
    public void Clone_ProducesEqualButDistinctInstance()
    {
        var original = MarkdownContent.Create("# hello");
        var clone = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = MarkdownContent.Create("hello");
        var b = MarkdownContent.Create("hello");

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var a = MarkdownContent.Create("hello");
        var b = MarkdownContent.Create("world");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ToDto_ContainsValueAndHtml()
    {
        var content = MarkdownContent.Create("**bold**");

        var dto = content.ToDto();

        dto.Value.Should().Be(content.Value);
        dto.Html.Should().Be(content.Html);
    }
}
