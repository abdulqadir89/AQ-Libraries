using AQ.Identity.UI.Localization;
using FluentAssertions;
using Xunit;

namespace AQ.Identity.UI.Tests.Localization;

public class UiLocaleMapperTests
{
    [Theory]
    // zh-TW group
    [InlineData("zh-tw", "zh-TW")]
    [InlineData("zh-hant", "zh-TW")]
    [InlineData("zh-hant-tw", "zh-TW")]
    // zh-HK group
    [InlineData("zh-hk", "zh-HK")]
    [InlineData("zh-mo", "zh-HK")]
    [InlineData("zh-hant-hk", "zh-HK")]
    [InlineData("zh-hant-mo", "zh-HK")]
    // zh-CN group
    [InlineData("zh-cn", "zh-CN")]
    [InlineData("zh-sg", "zh-CN")]
    [InlineData("zh-my", "zh-CN")]
    [InlineData("zh-hans", "zh-CN")]
    [InlineData("zh-hans-cn", "zh-CN")]
    [InlineData("zh", "zh-CN")]
    // en group
    [InlineData("en", "en")]
    [InlineData("en-US", "en")]
    [InlineData("en-GB", "en")]
    public void Map_KnownTag_ReturnsExpectedCulture(string tag, string expected)
    {
        UiLocaleMapper.Map(tag).Should().Be(expected);
    }

    [Theory]
    [InlineData("ZH-TW")]
    [InlineData("Zh-Hant")]
    [InlineData("EN")]
    public void Map_IsCaseInsensitive(string tag)
    {
        UiLocaleMapper.Map(tag).Should().NotBeNull();
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("de-DE")]
    [InlineData("")]
    [InlineData(null)]
    public void Map_UnmatchedOrEmpty_ReturnsNull(string? tag)
    {
        UiLocaleMapper.Map(tag).Should().BeNull();
    }

    [Fact]
    public void Map_MultipleTokens_ReturnsFirstMatch()
    {
        UiLocaleMapper.Map("fr zh-TW en").Should().Be("zh-TW");
    }

    [Fact]
    public void Map_MultipleTokens_NoneMatch_ReturnsNull()
    {
        UiLocaleMapper.Map("fr de ja").Should().BeNull();
    }

    [Fact]
    public void Map_FirstTokenWins_EvenIfLaterTokenAlsoMatches()
    {
        UiLocaleMapper.Map("zh-CN zh-TW").Should().Be("zh-CN");
    }
}
