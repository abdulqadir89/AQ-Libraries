using Ganss.Xss;
using Markdig;

namespace AQ.ValueObjects;

/// <summary>
/// Do not use that unless extremly necessary. Instead return Value or Html directly depending on the use case.
/// DTO for MarkdownContent responses in API endpoints, for the rare case both the
/// canonical Markdown and the rendered HTML are needed in the same response.
/// </summary>
public class MarkdownContentDto
{
    public string Value { get; set; } = default!;
    public string Html { get; set; } = default!;
}

public sealed class MarkdownContent : ValueObject
{
    public string Value { get; init; }
    public string Html { get; init; }

    // Factory method for creating a new instance from raw markdown
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

        return SanitizeHtml(rawHtml);
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
            "del", "s", "strike", "sub", "sup", "mark",
            // Tiptap: Youtube (iframe), Audio (audio/source), TaskList/TaskItem, Mathematics
            "iframe", "audio", "source", "input", "label"
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
        sanitizer.AllowedAttributes.Add("style");
        // Tiptap: Youtube (iframe) / Audio attributes
        sanitizer.AllowedAttributes.Add("allow");
        sanitizer.AllowedAttributes.Add("allowfullscreen");
        sanitizer.AllowedAttributes.Add("loading");
        sanitizer.AllowedAttributes.Add("frameborder");
        sanitizer.AllowedAttributes.Add("controls");
        // Tiptap: TaskList/TaskItem checkbox
        sanitizer.AllowedAttributes.Add("type");
        sanitizer.AllowedAttributes.Add("checked");
        sanitizer.AllowedAttributes.Add("disabled");
        // Tiptap: TextAlign reads/writes inline style="text-align:..."
        sanitizer.AllowedCssProperties.Add("text-align");

        // Tiptap: TaskList/TaskItem (data-type, data-checked) and Mathematics (data-type, data-latex)
        sanitizer.AllowedAttributes.Add("data-type");
        sanitizer.AllowedAttributes.Add("data-checked");
        sanitizer.AllowedAttributes.Add("data-latex");

        // Restrict allowed URI schemes for links and images
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("mailto");

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

    /// <summary>
    /// Converts this MarkdownContent to a DTO for API response serialization.
    /// </summary>
    public MarkdownContentDto ToDto()
    {
        return new MarkdownContentDto
        {
            Value = Value,
            Html = Html
        };
    }
}
