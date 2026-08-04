using System.Net;
using System.Text.RegularExpressions;
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

    // AI-generated markdown uses \(...\) / \[...\] for math (not $...$), since the LLM is
    // inconsistent about bare $ (currency vs. math). Convert to $...$ / $$...$$ before Markdig
    // runs its Mathematics extension, which only recognizes dollar delimiters. Skips fenced/inline
    // code spans so literal backslash-paren in code isn't mistaken for math.
    private static readonly Regex CodeSpanOrFence =
        new(@"```.*?```|`[^`\n]*`", RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex EscapedInlineMath =
        new(@"\\\((.+?)\\\)", RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex EscapedBlockMath =
        new(@"\\\[(.+?)\\\]", RegexOptions.Compiled | RegexOptions.Singleline);

    private static string ConvertEscapedDelimitersToDollar(string markdown)
    {
        var codeRanges = CodeSpanOrFence.Matches(markdown)
            .Select(m => (m.Index, End: m.Index + m.Length))
            .ToList();

        bool InCode(int index) => codeRanges.Any(r => index >= r.Index && index < r.End);

        // Markdig only treats $$...$$ as block (not inline) math when it stands alone on its
        // own line, so the replacement must force line breaks around the delimiters.
        markdown = EscapedBlockMath.Replace(markdown, m =>
            InCode(m.Index) ? m.Value : $"\n\n$$\n{m.Groups[1].Value}\n$$\n\n");

        markdown = EscapedInlineMath.Replace(markdown, m =>
            InCode(m.Index) ? m.Value : $"${m.Groups[1].Value}$");

        return markdown;
    }

    // Matches Markdig's Mathematics extension output: <span class="math">\(...\)</span> (inline)
    // and <div class="math">\[...\]</div> (block). Captures the delimiter-wrapped LaTeX body.
    private static readonly Regex InlineMathSpan =
        new(@"<span class=""math"">\\\((.*?)\\\)</span>", RegexOptions.Compiled | RegexOptions.Singleline);

    // Markdig's block output has whitespace/newlines around the \[ \] delimiters, e.g.
    // "<div class=\"math\">\n\[\nx = y\n\]</div>".
    private static readonly Regex BlockMathDiv =
        new(@"<div class=""math"">\s*\\\[(.*?)\\\]\s*</div>", RegexOptions.Compiled | RegexOptions.Singleline);

    private static string ConvertMarkdownToHtml(string value)
    {
        // Uses Markdig for Markdown to HTML conversion
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseMathematics()
            .DisableHtml()
            .Build();

        var html = Markdig.Markdown.ToHtml(ConvertEscapedDelimitersToDollar(value), pipeline);
        return AddTiptapMathAttributes(html);
    }

    // Adds Tiptap's expected data-type/data-latex attributes onto Markdig's math spans/divs,
    // keeping the original class + delimiter-wrapped text content intact so ReverseMarkdown's
    // native `class="math"` span reader (and our custom div reader) can still round-trip them.
    private static string AddTiptapMathAttributes(string html)
    {
        html = InlineMathSpan.Replace(html, m =>
        {
            // Markdig HTML-encodes the captured text content (e.g. "<" -> "&lt;"); decode it back
            // to raw LaTeX before re-encoding it for the data-latex attribute, or entities double-escape.
            var rawLatex = WebUtility.HtmlDecode(m.Groups[1].Value);
            var attrLatex = WebUtility.HtmlEncode(rawLatex);
            return $"<span class=\"math\" data-type=\"inline-math\" data-latex=\"{attrLatex}\">\\({m.Groups[1].Value}\\)</span>";
        });

        html = BlockMathDiv.Replace(html, m =>
        {
            var rawLatex = WebUtility.HtmlDecode(m.Groups[1].Value).Trim();
            var attrLatex = WebUtility.HtmlEncode(rawLatex);
            return $"<div class=\"math display\" data-type=\"block-math\" data-latex=\"{attrLatex}\">\\[{m.Groups[1].Value}\\]</div>";
        });

        return html;
    }

    private static string ConvertHtmlToMarkdown(string html)
    {
        var converter = new Converter();
        converter.RegisterReader("div", new MathDivReader());
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
