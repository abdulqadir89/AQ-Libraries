using Ganss.Xss;
using Markdig;
using ReverseMarkdown;

namespace AQ.ValueObjects;

public enum ContentFormat { Html, Markdown }

/// <summary>
/// Do not use that unless extremly necessary. Instead return Value directly in the common case.
/// DTO for HtmlContent responses in API endpoints, for the rare case both the
/// canonical HTML and a derived Markdown export are needed in the same response.
/// </summary>
public class HtmlContentDto
{
    public string Value { get; set; } = default!;
    public string Markdown { get; set; } = default!;
}

public sealed class HtmlContent : ValueObject
{
    public string Value { get; init; }

    // Factory method for creating a new instance from raw HTML (e.g. Tiptap editor output)
    public static HtmlContent FromHtml(string html)
    {
        return new HtmlContent(SanitizeHtml(html));
    }

    // Factory method for creating a new instance from markdown (e.g. AI-generated content)
    public static HtmlContent FromMarkdown(string markdown)
    {
        return new HtmlContent(SanitizeHtml(ConvertMarkdownToHtml(markdown)));
    }

    // parameter less constructor for EF Core
    private HtmlContent()
    {
        Value = default!;
    }

    private HtmlContent(string sanitizedHtml)
    {
        Value = sanitizedHtml;
    }

    // Lossy conversion for export/LLM-prompt-context use — not for storage round-trips
    public string ToMarkdown()
    {
        return ConvertHtmlToMarkdown(Value);
    }

    private static string ConvertMarkdownToHtml(string value)
    {
        // Uses Markdig for Markdown to HTML conversion
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml()
            .Build();

        return Markdown.ToHtml(value, pipeline);
    }

    private static string ConvertHtmlToMarkdown(string html)
    {
        var converter = new Converter();
        return converter.Convert(html);
    }

    private static string SanitizeHtml(string rawHtml)
    {
        // Sanitize the generated HTML using HtmlSanitizer
        var sanitizer = new HtmlSanitizer();

        // Configure sanitizer: allow common formatting tags and safe attributes
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedAttributes.Clear();

        var allowedTags = new[]
        {
            "a", "b", "i", "strong", "em", "u", "p", "ul", "ol", "li",
            "br", "hr", "blockquote", "code", "pre", "span", "div",
            "h1", "h2", "h3", "h4", "h5", "h6", "img", "table", "thead", "tbody", "tr", "th", "td",
            "del", "s", "strike",
            // Tiptap custom nodes: videoEmbed (iframe), audioEmbed (audio/source)
            "iframe", "audio", "source"
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
        // Tiptap custom nodes: videoEmbed (iframe) / audioEmbed (audio) attributes
        sanitizer.AllowedAttributes.Add("allow");
        sanitizer.AllowedAttributes.Add("allowfullscreen");
        sanitizer.AllowedAttributes.Add("loading");
        sanitizer.AllowedAttributes.Add("frameborder");
        sanitizer.AllowedAttributes.Add("controls");

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
    }

    public override HtmlContent Clone()
    {
        return FromHtml(Value);
    }

    /// <summary>
    /// Converts this HtmlContent to a DTO for API response serialization.
    /// </summary>
    public HtmlContentDto ToDto()
    {
        return new HtmlContentDto
        {
            Value = Value,
            Markdown = ToMarkdown()
        };
    }
}
