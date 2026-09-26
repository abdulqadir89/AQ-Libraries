namespace AQ.Identity.UI.Localization;

/// <summary>
/// Maps an OpenID Connect <c>ui_locales</c> value (a space-separated list of BCP-47 tags,
/// most-preferred first) to one of the four supported .NET UI culture names, per the
/// negotiation table in the localization plan §1.1.
/// </summary>
public static class UiLocaleMapper
{
    /// <summary>
    /// Splits <paramref name="uiLocales"/> on spaces and returns the .NET culture name
    /// (<c>en</c>, <c>zh-CN</c>, <c>zh-TW</c> or <c>zh-HK</c>) for the first token that maps.
    /// Returns <see langword="null"/> if nothing matches (the caller falls through to the
    /// next request-culture provider rather than defaulting to English here).
    /// </summary>
    public static string? Map(string? uiLocales)
    {
        if (string.IsNullOrWhiteSpace(uiLocales))
        {
            return null;
        }

        var tokens = uiLocales.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            var mapped = MapTag(token);
            if (mapped != null)
            {
                return mapped;
            }
        }

        return null;
    }

    private static string? MapTag(string tag)
    {
        var lower = tag.ToLowerInvariant();

        return lower switch
        {
            "zh-tw" or "zh-hant" or "zh-hant-tw" => "zh-TW",
            "zh-hk" or "zh-mo" or "zh-hant-hk" or "zh-hant-mo" => "zh-HK",
            "zh-cn" or "zh-sg" or "zh-my" or "zh" => "zh-CN",
            _ when lower.StartsWith("zh-hans", StringComparison.Ordinal) => "zh-CN",
            _ when lower.StartsWith("en", StringComparison.Ordinal) => "en",
            _ => null,
        };
    }
}
