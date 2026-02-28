namespace AQ.StateMachine.Entities;

public enum StateMachineDefinitionStatus
{
    Draft = 0,       // still being designed
    Published = 1,   // finalized, locked, and used in production
    Deprecated = 2,  // old version, no new instances should use it
    Archived = 3     // fully retired, kept only for audit/history
}

