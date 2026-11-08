using AQ.Actors;
using AQ.Events.Outbox;
using FluentAssertions;

namespace AQ.EntityFramework.Tests.Pipeline;

public class OutboxEventPostponeTests
{
    private static OutboxEvent NewEvent() =>
        new(OutboxEventCategory.Domain, "Some.Event", "{}", DateTimeOffset.UtcNow, new Actor(Guid.NewGuid(), PrincipalKind.Human));

    [Fact]
    public void Postpone_SchedulesAnotherTryWithoutCountingAnAttempt()
    {
        var outboxEvent = NewEvent();
        outboxEvent.RecordFailure("fault", TimeSpan.FromMinutes(1));

        outboxEvent.Postpone("lost a race", TimeSpan.FromMinutes(2));

        outboxEvent.ProcessingAttempts.Should().Be(1);
        outboxEvent.LastError.Should().Be("lost a race");
        outboxEvent.NextProcessingAttempt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(2), TimeSpan.FromSeconds(5));
        outboxEvent.IsProcessed.Should().BeFalse();
    }

    [Fact]
    public void Postpone_NeverRunsTheEventOutOfRetries()
    {
        var outboxEvent = NewEvent();

        for (var i = 0; i < 20; i++)
        {
            outboxEvent.Postpone("lost a race", TimeSpan.FromSeconds(1));
        }

        outboxEvent.HasExceededMaxRetries(5).Should().BeFalse();
        outboxEvent.ProcessingAttempts.Should().Be(0);
    }
}
