using AQ.Entities;
using AQ.Utilities.Results;
using AQ.Utilities.Revision;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.Tests.Revision;

public class RevisionExtensionsTests
{
    private sealed class Record(int revision) : ITimestamped
    {
        public DateTimeOffset CreatedAt => DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt => null;
        public int Revision { get; } = revision;
    }

    [Fact]
    public void EnsureRevision_MatchingRevision_Succeeds()
    {
        var result = new Record(3).EnsureRevision(3);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(0)]
    public void EnsureRevision_DifferentRevision_FailsWithConflict(int expected)
    {
        var result = new Record(3).EnsureRevision(expected);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be(RevisionExtensions.StaleCode);
        result.Error.ToHttpStatusCode().Should().Be(409);
    }
}
