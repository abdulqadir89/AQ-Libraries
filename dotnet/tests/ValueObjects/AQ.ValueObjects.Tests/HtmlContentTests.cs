using AQ.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AQ.ValueObjects.Tests;

public class HtmlContentTests
{
    [Fact]
    public void FromHtml_StripsScriptTags()
    {
        var content = HtmlContent.FromHtml("<p>hello</p><script>alert('xss')</script>");

        content.Value.Should().Contain("<p>hello</p>");
        content.Value.Should().NotContain("<script>");
    }

    [Fact]
    public void FromHtml_StripsOnClickAttribute()
    {
        var content = HtmlContent.FromHtml("<p onclick=\"alert('xss')\">hello</p>");

        content.Value.Should().NotContain("onclick");
    }

    [Fact]
    public void FromHtml_StripsJavascriptScheme()
    {
        var content = HtmlContent.FromHtml("<a href=\"javascript:alert('xss')\">click</a>");

        content.Value.Should().NotContain("javascript:");
    }

    [Fact]
    public void FromHtml_PreservesAllowlistedTags()
    {
        var content = HtmlContent.FromHtml("<p><strong>bold</strong> and <em>italic</em></p>");

        content.Value.Should().Contain("<strong>bold</strong>");
        content.Value.Should().Contain("<em>italic</em>");
    }

    [Fact]
    public void FromHtml_PreservesImageTag()
    {
        var content = HtmlContent.FromHtml("<p><img src=\"/api/attachments/foo.jpg\" alt=\"foo\"></p>");

        content.Value.Should().Contain("<img");
        content.Value.Should().Contain("src=\"/api/attachments/foo.jpg\"");
    }

    [Fact]
    public void FromHtml_EmptyInput_ProducesEmptyOutput()
    {
        var content = HtmlContent.FromHtml("");

        content.Value.Should().BeEmpty();
    }

    [Fact]
    public void FromHtml_IsIdempotentOnCleanInput()
    {
        var first = HtmlContent.FromHtml("<p>clean</p>");
        var second = HtmlContent.FromHtml(first.Value);

        second.Value.Should().Be(first.Value);
    }

    [Fact]
    public void FromMarkdown_ConvertsHeadingsAndBold()
    {
        var content = HtmlContent.FromMarkdown("# Title\n\n**bold** text");

        content.Value.Should().Contain("<h1>Title</h1>");
        content.Value.Should().Contain("<strong>bold</strong>");
    }

    [Fact]
    public void FromMarkdown_ConvertsListsAndLinks()
    {
        var content = HtmlContent.FromMarkdown("- item one\n- item two\n\n[link](https://example.com)");

        content.Value.Should().Contain("<ul>");
        content.Value.Should().Contain("<li>item one</li>");
        content.Value.Should().Contain("href=\"https://example.com\"");
    }

    [Fact]
    public void FromMarkdown_ConvertsTables()
    {
        var content = HtmlContent.FromMarkdown("| A | B |\n|---|---|\n| 1 | 2 |");

        content.Value.Should().Contain("<table>");
    }

    [Fact]
    public void FromMarkdown_StripsRawHtmlEmbeddedInMarkdown()
    {
        var content = HtmlContent.FromMarkdown("plain text <script>alert('xss')</script>");

        content.Value.Should().NotContain("<script>");
    }

    [Fact]
    public void ToMarkdown_ProducesReasonableMarkdownFromHtml()
    {
        var content = HtmlContent.FromHtml("<p><strong>bold</strong></p>");

        content.ToMarkdown().Should().Contain("bold");
    }

    [Fact]
    public void ToMarkdown_IsLossy_RoundTripNotGuaranteedToMatchOriginal()
    {
        const string original = "# Title\n\n**bold** text with *emphasis*";

        var roundTripped = HtmlContent.FromMarkdown(original).ToMarkdown();

        // Documenting the lossy contract: HTML->Markdown re-derivation is not
        // required to reproduce the exact original markdown source.
        roundTripped.Should().NotBe(original);
    }

    [Fact]
    public void Clone_ProducesEqualButDistinctInstance()
    {
        var original = HtmlContent.FromHtml("<p>hello</p>");
        var clone = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = HtmlContent.FromHtml("<p>hello</p>");
        var b = HtmlContent.FromHtml("<p>hello</p>");

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var a = HtmlContent.FromHtml("<p>hello</p>");
        var b = HtmlContent.FromHtml("<p>world</p>");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ToDto_ContainsValueAndDerivedMarkdown()
    {
        var content = HtmlContent.FromHtml("<p><strong>bold</strong></p>");

        var dto = content.ToDto();

        dto.Value.Should().Be(content.Value);
        dto.Markdown.Should().Be(content.ToMarkdown());
    }

    [Fact]
    public void FromHtml_PreservesVideoEmbedIframe()
    {
        var content = HtmlContent.FromHtml(
            "<div class=\"rme-video-embed\"><iframe src=\"https://www.youtube-nocookie.com/embed/abc123\" allow=\"autoplay\" allowfullscreen loading=\"lazy\" frameborder=\"0\"></iframe></div>");

        content.Value.Should().Contain("<iframe");
        content.Value.Should().Contain("src=\"https://www.youtube-nocookie.com/embed/abc123\"");
    }

    [Fact]
    public void FromHtml_PreservesAudioEmbed()
    {
        var content = HtmlContent.FromHtml(
            "<audio controls=\"true\"><source src=\"https://example.com/audio.mp3\"></audio>");

        content.Value.Should().Contain("<audio");
        content.Value.Should().Contain("<source");
    }

    [Fact]
    public void FromMarkdown_ConvertsInlineMathToTiptapSpan()
    {
        var content = HtmlContent.FromMarkdown("Simplify $3^{6n+15}$ please.");

        content.Value.Should().Contain("data-type=\"inline-math\"");
        content.Value.Should().Contain("data-latex=\"3^{6n+15}\"");
    }

    [Fact]
    public void FromMarkdown_ConvertsBlockMathToTiptapDiv()
    {
        // $$ must be alone on its own line to trigger Markdig's block (not inline) math rule.
        var content = HtmlContent.FromMarkdown("$$\nx = \\frac{-b \\pm \\sqrt{b^2-4ac}}{2a}\n$$");

        content.Value.Should().Contain("data-type=\"block-math\"");
        content.Value.Should().Contain("data-latex=\"x = \\frac{-b \\pm \\sqrt{b^2-4ac}}{2a}\"");
    }

    [Fact]
    public void FromMarkdown_EscapesSpecialCharactersInLatexAttribute()
    {
        var content = HtmlContent.FromMarkdown("Compare $a < b > c \\text{\"quoted\"}$ here.");

        content.Value.Should().NotContain("data-latex=\"a < b");
        content.Value.Should().Contain("data-latex=\"a &lt; b &gt; c");
        content.Value.Should().Contain("&quot;quoted&quot;");
    }

    [Fact]
    public void FromMarkdown_NonMathContentUnaffectedByMathExtension()
    {
        var content = HtmlContent.FromMarkdown("# Title\n\n**bold** text, cost is $5 total.");

        content.Value.Should().Contain("<h1>Title</h1>");
        content.Value.Should().Contain("<strong>bold</strong>");
    }

    [Fact]
    public void FromMarkdown_ConvertsEscapedInlineParensToMath()
    {
        // AI-generated content uses \(...\) instead of $...$ since the LLM is inconsistent
        // about bare $ (currency vs. math).
        var content = HtmlContent.FromMarkdown("Simplify \\(3^{6n+15}\\) please.");

        content.Value.Should().Contain("data-type=\"inline-math\"");
        content.Value.Should().Contain("data-latex=\"3^{6n+15}\"");
    }

    [Fact]
    public void FromMarkdown_ConvertsEscapedBlockBracketsToMath()
    {
        var content = HtmlContent.FromMarkdown("\\[x = \\frac{-b \\pm \\sqrt{b^2-4ac}}{2a}\\]");

        content.Value.Should().Contain("data-type=\"block-math\"");
        content.Value.Should().Contain("data-latex=\"x = \\frac{-b \\pm \\sqrt{b^2-4ac}}{2a}\"");
    }

    [Fact]
    public void FromMarkdown_UndelimitedCurrencyDollarNeverBecomesMath()
    {
        var content = HtmlContent.FromMarkdown("The item costs $5 and shipping is $10 extra.");

        content.Value.Should().NotContain("data-type=\"inline-math\"");
        content.Value.Should().Contain("$5");
        content.Value.Should().Contain("$10");
    }

    [Fact]
    public void FromMarkdown_EscapedParensInsideCodeSpanNotConvertedToMath()
    {
        var content = HtmlContent.FromMarkdown("Use the pattern `\\(foo\\)` in your regex.");

        content.Value.Should().NotContain("data-type=\"inline-math\"");
        content.Value.Should().Contain("\\(foo\\)");
    }

    [Fact]
    public void ToMarkdown_RoundTripsInlineMathWithoutLosingLatex()
    {
        var content = HtmlContent.FromMarkdown("Simplify $3^{6n+15}$ please.");

        // ReverseMarkdown's math writer emits \(...\) rather than $...$ — both are
        // valid input Markdig accepts on the way back in. The bar is "not silently dropped".
        content.ToMarkdown().Should().Contain("3^{6n+15}");
    }

    [Fact]
    public void ToMarkdown_RoundTripsBlockMathWithoutLosingLatex()
    {
        var content = HtmlContent.FromMarkdown("$$\nx = y + 1\n$$");

        content.ToMarkdown().Should().Contain("x = y + 1");
    }
}
