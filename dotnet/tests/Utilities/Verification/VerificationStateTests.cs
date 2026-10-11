using AQ.Entities;
using AQ.Utilities.Verification;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.Tests.Verification;

public class VerificationStateTests
{
    [Fact]
    public void NewState_IsEmpty()
    {
        var state = new VerificationState();

        state.Count.Should().Be(0);
        state.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Add_IncrementsCountAndRecordsEntry()
    {
        var state = new VerificationState();
        var userId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        state.Add(userId);

        state.Count.Should().Be(1);
        state.Entries.Should().ContainSingle();
        state.Entries[0].VerifiedByUserId.Should().Be(userId);
        state.Entries[0].VerifiedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Add_AccumulatesAcrossUsers()
    {
        var state = new VerificationState();

        state.Add(Guid.NewGuid());
        state.Add(Guid.NewGuid());

        state.Count.Should().Be(2);
        state.Entries.Should().HaveCount(2);
    }

    [Fact]
    public void HasBeenVerifiedBy_ReturnsTrueOnlyForUsersWhoVerified()
    {
        var state = new VerificationState();
        var verifier = Guid.NewGuid();
        state.Add(verifier);

        state.HasBeenVerifiedBy(verifier).Should().BeTrue();
        state.HasBeenVerifiedBy(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Reset_ClearsCountAndEntriesAndAllowsReverification()
    {
        var state = new VerificationState();
        var verifier = Guid.NewGuid();
        state.Add(verifier);

        state.Reset();

        state.Count.Should().Be(0);
        state.Entries.Should().BeEmpty();
        state.HasBeenVerifiedBy(verifier).Should().BeFalse();
    }

    [Fact]
    public void Reset_ReplacesEntriesList_SoPreviousReferenceIsNotMutated()
    {
        var state = new VerificationState();
        state.Add(Guid.NewGuid());
        var previous = state.Entries;

        state.Reset();
        state.Add(Guid.NewGuid());

        previous.Should().HaveCount(1);
        state.Entries.Should().HaveCount(1);
        state.Entries.Should().NotBeSameAs(previous);
    }
}
