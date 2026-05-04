using AQ.Entities;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Abstract base for rules that map a state machine transition (by ToStateId) to a
/// downstream effect. Concrete subclasses define what effect is applied.
/// </summary>
public abstract class StateMachineTransitionRule : Entity
{
    public Guid DefinitionId { get; protected set; }
    public int DefinitionVersion { get; protected set; }
    public Guid ToStateId { get; protected set; }
    public string? RejectionMessage { get; protected set; }

    protected StateMachineTransitionRule() { }

    protected StateMachineTransitionRule(
        Guid definitionId,
        int definitionVersion,
        Guid toStateId,
        string? rejectionMessage)
    {
        DefinitionId = definitionId;
        DefinitionVersion = definitionVersion;
        ToStateId = toStateId;
        RejectionMessage = rejectionMessage?.Trim();
    }
}
