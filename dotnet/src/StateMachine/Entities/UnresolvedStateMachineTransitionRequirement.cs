namespace AQ.StateMachine.Entities;

/// <summary>
/// A stored requirement whose type is not registered. It keeps its name and data untouched so saving the
/// transition again does not lose it, and no handler fulfils it.
/// </summary>
public sealed record UnresolvedStateMachineTransitionRequirement(string RequirementTypeName, string DataJson)
    : IStateMachineTransitionRequirement;
