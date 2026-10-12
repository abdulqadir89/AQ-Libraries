using AQ.Utilities.Review;
using AQ.Utilities.Verification;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.Tests.Review;

public class ReviewStateTests
{
    private sealed class Reviewable : IReviewable
    {
        public ReviewState Review { get; } = new();
    }

    private sealed class ReviewableAndVerifiable : IReviewable, IVerifiable
    {
        public ReviewState Review { get; } = new();
        public VerificationState Verification { get; } = new();
    }

    [Fact]
    public void NewState_IsEmptyAndNeverDueOrCurrent()
    {
        var state = new ReviewState();

        state.NextReviewDue.Should().BeNull();
        state.LastReviewedAt.Should().BeNull();
        state.IsDue(DateTimeOffset.UtcNow).Should().BeFalse();
        state.IsCurrent().Should().BeFalse();
    }

    [Fact]
    public void MarkReviewed_RecordsReviewerActivityAndNextDue()
    {
        var state = new ReviewState();
        var userId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var nextDue = DateTimeOffset.UtcNow.AddDays(90);

        state.MarkReviewed(userId, nextDue, activityId);

        state.LastReviewedById.Should().Be(userId);
        state.LastReviewActivityId.Should().Be(activityId);
        state.NextReviewDue.Should().Be(nextDue);
        state.LastReviewedAt.Should().NotBeNull();
        state.IsCurrent().Should().BeTrue();
    }

    [Fact]
    public void IsDue_TrueOnlyOnceNextDueHasPassed()
    {
        var state = new ReviewState();
        state.SetNextReviewDue(DateTimeOffset.UtcNow.AddDays(1));

        state.IsDue(DateTimeOffset.UtcNow).Should().BeFalse();
        state.IsDue(DateTimeOffset.UtcNow.AddDays(2)).Should().BeTrue();
    }

    [Fact]
    public void RecordNotified_SuppressesDueUntilScheduleChanges()
    {
        var state = new ReviewState();
        var due = DateTimeOffset.UtcNow.AddMinutes(-5);
        state.SetNextReviewDue(due);
        state.IsDue(DateTimeOffset.UtcNow).Should().BeTrue();

        state.RecordNotified();
        state.IsDue(DateTimeOffset.UtcNow).Should().BeFalse();

        state.SetNextReviewDue(DateTimeOffset.UtcNow.AddMinutes(-1));
        state.LastNotifiedAt.Should().BeNull();
        state.IsDue(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void MarkReviewed_ClearsNotifiedMarker()
    {
        var state = new ReviewState();
        state.SetNextReviewDue(DateTimeOffset.UtcNow.AddMinutes(-5));
        state.RecordNotified();

        state.MarkReviewed(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(30));

        state.LastNotifiedAt.Should().BeNull();
    }

    [Fact]
    public void IsCurrent_FalseWhenVerificationWasResetAfterReview()
    {
        var state = new ReviewState();
        var verification = new VerificationState();
        state.MarkReviewed(Guid.NewGuid(), null);
        state.IsCurrent(verification).Should().BeTrue();

        Thread.Sleep(5);
        verification.Reset();

        state.IsCurrent(verification).Should().BeFalse();
    }

    [Fact]
    public void IsCurrent_TrueWhenReviewedAfterTheLastReset()
    {
        var state = new ReviewState();
        var verification = new VerificationState();
        verification.Reset();
        Thread.Sleep(5);

        state.MarkReviewed(Guid.NewGuid(), null);

        state.IsCurrent(verification).Should().BeTrue();
    }

    [Fact]
    public void Clear_RemovesEverything()
    {
        var state = new ReviewState();
        state.MarkReviewed(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), Guid.NewGuid());

        state.Clear();

        state.NextReviewDue.Should().BeNull();
        state.LastReviewedAt.Should().BeNull();
        state.LastReviewedById.Should().BeNull();
        state.LastReviewActivityId.Should().BeNull();
    }

    [Fact]
    public void Extension_OnVerifiableEntity_AddsOneVerificationAndReturnsTrue()
    {
        var entity = new ReviewableAndVerifiable();
        var userId = Guid.NewGuid();

        var added = entity.MarkReviewed(userId, null);

        added.Should().BeTrue();
        entity.Verification.Count.Should().Be(1);
        entity.Review.LastReviewedById.Should().Be(userId);
    }

    [Fact]
    public void Extension_WhenUserAlreadyVerified_DoesNotDuplicateAndReturnsFalse()
    {
        var entity = new ReviewableAndVerifiable();
        var userId = Guid.NewGuid();
        entity.Verification.Add(userId);

        var added = entity.MarkReviewed(userId, null);

        added.Should().BeFalse();
        entity.Verification.Count.Should().Be(1);
        entity.Review.LastReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void Extension_OnNonVerifiableEntity_OnlyUpdatesReview()
    {
        var entity = new Reviewable();

        var added = entity.MarkReviewed(Guid.NewGuid(), null);

        added.Should().BeFalse();
        entity.Review.LastReviewedAt.Should().NotBeNull();
        entity.IsReviewCurrent().Should().BeTrue();
    }
}
