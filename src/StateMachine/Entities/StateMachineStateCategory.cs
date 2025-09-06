namespace AQ.StateMachineEntities;

/// <summary>
/// Defines the status of a state within a state machine.
/// </summary>
public enum StateMachineStateCategory
{
    /// <summary>
    /// Regular intermediate state.
    /// </summary>
    Intermediate,

    /// <summary>
    /// Initial state - starting point of the state machine.
    /// </summary>
    Initial,

    /// <summary>
    /// Final state - end point of the state machine.
    /// </summary>
    Final
}
