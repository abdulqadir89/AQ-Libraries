using AQ.Entities;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Abstract base for rules that govern what is allowed when a state machine instance
/// is currently IN a given state. Concrete subclasses add domain-specific constraints.
/// </summary>
public abstract class StateMachineStateRule : Entity
{
    public Guid DefinitionId { get; protected set; }
    public int DefinitionVersion { get; protected set; }
    public Guid StateId { get; protected set; }
    public string? RejectionMessage { get; protected set; }

    protected StateMachineStateRule() { }

    protected StateMachineStateRule(
        Guid definitionId,
        int definitionVersion,
        Guid stateId,
        string? rejectionMessage)
    {
        DefinitionId = definitionId;
        DefinitionVersion = definitionVersion;
        StateId = stateId;
        RejectionMessage = rejectionMessage?.Trim();
    }
}
