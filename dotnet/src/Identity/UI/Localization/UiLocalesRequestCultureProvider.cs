using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.WebUtilities;

namespace AQ.Identity.UI.Localization;

/// <summary>
/// Resolves the request culture from the OpenID Connect <c>ui_locales</c> query parameter,
/// so the Identity UI opens in the language the relying-party app was using when it started
/// the login redirect. Falls back to <c>ui_locales</c> embedded in a <c>ReturnUrl</c> query
/// value (the shape used by Razor Pages' own login-challenge redirects, e.g.
/// <c>/auth/login?ReturnUrl=%2Fconnect%2Fauthorize%3F...%26ui_locales%3Dzh-TW</c>).
/// </summary>
public sealed class UiLocalesRequestCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var uiLocales = httpContext.Request.Query["ui_locales"].ToString();

        if (string.IsNullOrEmpty(uiLocales))
        {
            var returnUrl = httpContext.Request.Query["ReturnUrl"].ToString();
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = httpContext.Request.Query["returnUrl"].ToString();
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                uiLocales = ExtractUiLocalesFromReturnUrl(returnUrl);
            }
        }

        var mapped = UiLocaleMapper.Map(uiLocales);
        if (mapped is null)
        {
            return NoMatchResult;
        }

        var result = new ProviderCultureResult(mapped, mapped);
        return Task.FromResult<ProviderCultureResult?>(result);
    }

    private static string? ExtractUiLocalesFromReturnUrl(string returnUrl)
    {
        // ReturnUrl may be a relative path+query (typical) or an absolute URL. Uri needs a
        // base to parse a relative value, so anchor it to a dummy authority purely to reach
        // the query string — the host/scheme themselves are discarded.
        var query = returnUrl.StartsWith('/')
            ? new Uri("http://localhost" + returnUrl, UriKind.Absolute).Query
            : Uri.TryCreate(returnUrl, UriKind.Absolute, out var absolute)
                ? absolute.Query
                : null;

        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        var parsed = QueryHelpers.ParseQuery(query);
        return parsed.TryGetValue("ui_locales", out var values) ? values.ToString() : null;
    }

    private static readonly Task<ProviderCultureResult?> NoMatchResult =
        Task.FromResult<ProviderCultureResult?>(null);
}
