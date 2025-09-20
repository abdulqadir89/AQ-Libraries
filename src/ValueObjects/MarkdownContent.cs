using Ganss.Xss;
using Markdig;

namespace AQ.ValueObjects;

public sealed class MarkdownContent : ValueObject
{
    public string Value { get; init; }
    public string Html { get; init; }

    // Factory method for creating a new instance
    public static MarkdownContent Create(string value)
    {
        return new MarkdownContent(value);
    }

    // parameter less constructor for EF Core
    private MarkdownContent()
    {
        Value = default!;
        Html = default!;
    }

    private MarkdownContent(string value)
    {
        Value = value;
        Html = GenerateHtml(value);
    }

    private static string GenerateHtml(string value)
    {
        // Uses Markdig for Markdown to HTML conversion
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml()
            .Build();

        var rawHtml = Markdown.ToHtml(value, pipeline);

        // Sanitize the generated HTML using HtmlSanitizer
        var sanitizer = new HtmlSanitizer();

        // Configure sanitizer: allow common formatting tags and safe attributes
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedAttributes.Clear();

        var allowedTags = new[]
        {
            "a", "b", "i", "strong", "em", "u", "p", "ul", "ol", "li",
            "br", "hr", "blockquote", "code", "pre", "span", "div",
            "h1", "h2", "h3", "h4", "h5", "h6", "img", "table", "thead", "tbody", "tr", "th", "td"
        };

        foreach (var t in allowedTags)
            sanitizer.AllowedTags.Add(t);

        // Allow href on anchors and src/alt/width/height on images
        sanitizer.AllowedAttributes.Add("href");
        sanitizer.AllowedAttributes.Add("src");
        sanitizer.AllowedAttributes.Add("alt");
        sanitizer.AllowedAttributes.Add("title");
        sanitizer.AllowedAttributes.Add("width");
        sanitizer.AllowedAttributes.Add("height");
        sanitizer.AllowedAttributes.Add("class");

        // Restrict allowed URI schemes for links and images
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("mailto");

        // Remove any potentially dangerous css properties
        sanitizer.AllowDataAttributes = false;

        return sanitizer.Sanitize(rawHtml);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
        yield return Html;
    }

    public override MarkdownContent Clone()
    {
        return Create(Value);

    }
}
