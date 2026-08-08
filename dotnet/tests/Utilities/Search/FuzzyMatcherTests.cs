using AQ.Utilities.Search;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.Tests.Search;

public class FuzzyMatcherTests
{
    [Theory]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("", "abc", 3)]
    [InlineData("abc", "", 3)]
    [InlineData("", "", 0)]
    [InlineData("same", "same", 0)]
    public void LevenshteinDistance_ReturnsExpectedDistance(string source, string target, int expected)
    {
        FuzzyMatcher.LevenshteinDistance(source, target).Should().Be(expected);
    }

    [Fact]
    public void SimilarityRatio_IdenticalStrings_ReturnsOne()
    {
        FuzzyMatcher.SimilarityRatio("hello", "hello").Should().Be(1.0);
    }

    [Fact]
    public void SimilarityRatio_CompletelyDifferentStrings_ReturnsLowScore()
    {
        var score = FuzzyMatcher.SimilarityRatio("abc", "xyz");
        score.Should().BeLessThan(0.5);
    }

    [Theory]
    [InlineData("John", "Jhon", 0.5, true)]
    [InlineData("John", "CompletelyDifferent", 0.6, false)]
    public void IsSimilar_RespectsThreshold(string source, string target, double threshold, bool expected)
    {
        FuzzyMatcher.IsSimilar(source, target, threshold).Should().Be(expected);
    }

    [Fact]
    public void JaroWinklerSimilarity_KnownPair_MatchesExpectedScore()
    {
        var score = FuzzyMatcher.JaroWinklerSimilarity("martha", "marhta");
        score.Should().BeApproximately(0.961, 0.01);
    }

    [Fact]
    public void Soundex_KnownValue_ReturnsExpectedCode()
    {
        FuzzyMatcher.Soundex("Robert").Should().Be("R163");
    }

    [Fact]
    public void Soundex_SimilarSoundingNames_ProduceSameCode()
    {
        FuzzyMatcher.Soundex("John").Should().Be(FuzzyMatcher.Soundex("Jhon"));
    }

    [Fact]
    public void CombinedFuzzyScore_ReturnsValueWithinRange()
    {
        var score = FuzzyMatcher.CombinedFuzzyScore("hello", "hallo");
        score.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void CombinedFuzzyScore_WrongWeightsLength_Throws()
    {
        var act = () => FuzzyMatcher.CombinedFuzzyScore("a", "b", new[] { 1.0, 2.0 });
        act.Should().Throw<ArgumentException>();
    }
}
