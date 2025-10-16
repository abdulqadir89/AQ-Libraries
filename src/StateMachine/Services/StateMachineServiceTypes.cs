using AQ.StateMachine.Entities;

namespace AQ.StateMachine.Services;

/// <summary>
/// Information about a state machine transition operation.
/// Used for passing transition context to effect handlers.
/// </summary>
public class StateMachineTransitionInfo
{
    public bool Success { get; set; }
    public Guid? PreviousStateId { get; set; }
    public Guid? NewStateId { get; set; }
    public Guid? TriggerId { get; set; }
    public bool WasForced { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset TransitionedAt { get; set; }
}

/// <summary>
/// Summary of requirement evaluation for a transition.
/// </summary>
public class RequirementEvaluationSummary
{
    public bool AllRequirementsMet { get; set; }
    public IEnumerable<RequirementEvaluationStatus> RequirementResults { get; set; } = [];
    public IEnumerable<string> FailureReasons { get; set; } = [];
}
