using AQ.Utilities.PeriodicReview;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.Tests.PeriodicReview;

public class ReviewStateTests
{
    private sealed class Reviewable : IPeriodicReviewable<PeriodicReviewState>
    {
        public PeriodicReviewState PeriodicReview { get; } = new();
    }

    private sealed class FixedCalculator(DateTimeOffset? due) : IPeriodicReviewDueCalculator<Reviewable>
    {
        public int Calls { get; private set; }

        public ValueTask<DateTimeOffset?> CalculateAsync(Reviewable entity, DateTimeOffset reviewedAt, CancellationToken cancellationToken = default)
        {
            Calls++;
            return ValueTask.FromResult(due);
        }
    }

    [Fact]
    public void NewState_IsEmptyAndNeverOverdue()
    {
        var state = new PeriodicReviewState();

        state.NextReviewDue.Should().BeNull();
        state.LastReviewedAt.Should().BeNull();
        state.IsOverdue(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void MarkReviewed_RecordsReviewDateAndNextDue()
    {
        var state = new PeriodicReviewState();
        var reviewedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var nextDue = DateTimeOffset.UtcNow.AddDays(90);

        state.MarkReviewed(reviewedAt, nextDue);

        state.LastReviewedAt.Should().Be(reviewedAt);
        state.NextReviewDue.Should().Be(nextDue);
    }

    [Fact]
    public void MarkReviewed_WithNullNextDue_KeepsFutureScheduleAndClearsPastOne()
    {
        var now = DateTimeOffset.UtcNow;
        var future = now.AddDays(30);
        var scheduled = new PeriodicReviewState();
        scheduled.SetNextReviewDue(future);
        scheduled.MarkReviewed(now, null);
        scheduled.NextReviewDue.Should().Be(future);

        var overdue = new PeriodicReviewState();
        overdue.SetNextReviewDue(now.AddDays(-1));
        overdue.MarkReviewed(now, null);
        overdue.NextReviewDue.Should().BeNull();
    }

    [Fact]
    public void IsOverdue_TrueOnlyOnceNextDueHasPassed()
    {
        var state = new PeriodicReviewState();
        state.SetNextReviewDue(DateTimeOffset.UtcNow.AddDays(1));

        state.IsOverdue(DateTimeOffset.UtcNow).Should().BeFalse();
        state.IsOverdue(DateTimeOffset.UtcNow.AddDays(2)).Should().BeTrue();
    }

    [Fact]
    public void Clear_RemovesBothDates()
    {
        var state = new PeriodicReviewState();
        state.MarkReviewed(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));

        state.Clear();

        state.NextReviewDue.Should().BeNull();
        state.LastReviewedAt.Should().BeNull();
    }

    [Fact]
    public async Task MarkReviewedAsync_ExplicitDueWinsOverCalculator()
    {
        var entity = new Reviewable();
        var explicitDue = DateTimeOffset.UtcNow.AddDays(10);
        var calculator = new FixedCalculator(DateTimeOffset.UtcNow.AddDays(180));

        await entity.MarkReviewedAsync(calculator, nextReviewDue: explicitDue);

        entity.PeriodicReview.NextReviewDue.Should().Be(explicitDue);
        calculator.Calls.Should().Be(0);
    }

    [Fact]
    public async Task MarkReviewedAsync_WithoutExplicitDue_UsesCalculator()
    {
        var entity = new Reviewable();
        var reviewedAt = DateTimeOffset.UtcNow;
        var calculated = reviewedAt.AddDays(180);

        await entity.MarkReviewedAsync(new FixedCalculator(calculated), reviewedAt);

        entity.PeriodicReview.LastReviewedAt.Should().Be(reviewedAt);
        entity.PeriodicReview.NextReviewDue.Should().Be(calculated);
    }

    [Fact]
    public async Task MarkReviewedAsync_WhenCalculatorHasNoRule_KeepsStillFutureDate()
    {
        var entity = new Reviewable();
        var future = DateTimeOffset.UtcNow.AddDays(30);
        entity.PeriodicReview.SetNextReviewDue(future);

        await entity.MarkReviewedAsync(new FixedCalculator(null));

        entity.PeriodicReview.NextReviewDue.Should().Be(future);
    }
}
