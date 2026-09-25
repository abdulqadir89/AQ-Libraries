using AQ.Identity.UI.Localization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AQ.Identity.UI.Tests.Localization;

public class UiLocalesRequestCultureProviderTests
{
    private readonly UiLocalesRequestCultureProvider _provider = new();

    private static HttpContext ContextWithQuery(string queryString)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(queryString);
        return context;
    }

    [Fact]
    public async Task DetermineProviderCultureResult_DirectUiLocalesQuery_ReturnsMappedCulture()
    {
        var context = ContextWithQuery("?ui_locales=zh-TW");

        var result = await _provider.DetermineProviderCultureResult(context);

        result.Should().NotBeNull();
        result!.Cultures[0].Value.Should().Be("zh-TW");
        result.UICultures[0].Value.Should().Be("zh-TW");
    }

    [Fact]
    public async Task DetermineProviderCultureResult_DirectUiLocalesQuery_MultipleTokens_ReturnsFirstMatch()
    {
        var context = ContextWithQuery("?ui_locales=" + Uri.EscapeDataString("fr zh-HK"));

        var result = await _provider.DetermineProviderCultureResult(context);

        result.Should().NotBeNull();
        result!.Cultures[0].Value.Should().Be("zh-HK");
    }

    [Fact]
    public async Task DetermineProviderCultureResult_NoUiLocalesAnywhere_ReturnsNull()
    {
        var context = ContextWithQuery("?foo=bar");

        var result = await _provider.DetermineProviderCultureResult(context);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DetermineProviderCultureResult_UiLocalesEmbeddedInReturnUrl_ReturnsMappedCulture()
    {
        // Mirrors a real login-challenge redirect: /auth/login?ReturnUrl=/connect/authorize?...&ui_locales=zh-CN
        var innerQuery = "client_id=abc&ui_locales=zh-CN";
        var returnUrl = "/connect/authorize?" + innerQuery;
        var context = ContextWithQuery("?ReturnUrl=" + Uri.EscapeDataString(returnUrl));

        var result = await _provider.DetermineProviderCultureResult(context);

        result.Should().NotBeNull();
        result!.Cultures[0].Value.Should().Be("zh-CN");
    }

    [Fact]
    public async Task DetermineProviderCultureResult_UiLocalesEmbeddedInLowercaseReturnUrl_ReturnsMappedCulture()
    {
        var returnUrl = "/connect/authorize?ui_locales=zh-HK";
        var context = ContextWithQuery("?returnUrl=" + Uri.EscapeDataString(returnUrl));

        var result = await _provider.DetermineProviderCultureResult(context);

        result.Should().NotBeNull();
        result!.Cultures[0].Value.Should().Be("zh-HK");
    }

    [Fact]
    public async Task DetermineProviderCultureResult_DirectQueryTakesPrecedenceOverReturnUrl()
    {
        var returnUrl = "/connect/authorize?ui_locales=zh-CN";
        var context = ContextWithQuery(
            "?ui_locales=zh-TW&ReturnUrl=" + Uri.EscapeDataString(returnUrl));

        var result = await _provider.DetermineProviderCultureResult(context);

        result.Should().NotBeNull();
        result!.Cultures[0].Value.Should().Be("zh-TW");
    }

    [Fact]
    public async Task DetermineProviderCultureResult_ReturnUrlWithoutUiLocales_ReturnsNull()
    {
        var returnUrl = "/connect/authorize?client_id=abc";
        var context = ContextWithQuery("?ReturnUrl=" + Uri.EscapeDataString(returnUrl));

        var result = await _provider.DetermineProviderCultureResult(context);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DetermineProviderCultureResult_UnmappableUiLocales_ReturnsNull()
    {
        var context = ContextWithQuery("?ui_locales=fr-FR");

        var result = await _provider.DetermineProviderCultureResult(context);

        result.Should().BeNull();
    }
}
