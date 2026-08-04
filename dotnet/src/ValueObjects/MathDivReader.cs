using AngleSharp.Dom;
using ReverseMarkdown.Dom;
using ReverseMarkdown.Readers;

namespace AQ.ValueObjects;

/// <summary>
/// ReverseMarkdown has no built-in `div`-based math detection (only `span.math`, used for
/// Markdig's inline math output). This reader restores `$$...$$` for block math divs
/// (`class="math display"`, emitted by <see cref="HtmlContent"/> for Tiptap block-math nodes)
/// and otherwise falls back to reading children, since ToMarkdown() is a lossy/best-effort export.
/// </summary>
internal sealed class MathDivReader : IMdReader
{
    public void Read(IElement element, ReaderContext ctx)
    {
        var className = element.GetAttribute("class") ?? string.Empty;
        if (className.Contains("math"))
        {
            var latex = element.TextContent.Trim();
            latex = latex.TrimStart('\\', '[').TrimEnd('\\', ']').Trim();
            ctx.Emit(new MdMath(latex, display: true));
            return;
        }

        ctx.ReadChildren(element);
    }
}
