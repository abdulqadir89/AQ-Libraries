namespace AQ.StateMachine.Entities;

/// <summary>
/// Summary information about a state machine instance.
/// </summary>
public class StateMachineSummary
{
    public Guid StateMachineId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string CurrentState { get; set; } = default!;
    public bool IsInFinalState { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastTransitionAt { get; set; }
    public int TotalTransitions { get; set; }
    public TimeSpan? TimeSinceLastTransition { get; set; }
    public Guid? ContextEntityId { get; set; }
    public string? ContextEntityType { get; set; }
    public IEnumerable<string> AvailableTriggers { get; set; } = [];
    public IEnumerable<StateMachineTransitionSummary> RecentTransitions { get; set; } = [];
}

/// <summary>
/// Summary information about a state transition.
/// </summary>
public class StateMachineTransitionSummary
{
    public DateTimeOffset TransitionedAt { get; set; }
    public string FromState { get; set; } = default!;
    public string ToState { get; set; } = default!;
    public string Trigger { get; set; } = default!;
    public Guid? TriggeredBy { get; set; }
    public bool WasForced { get; set; }
    public string? ForcedReason { get; set; }
}

