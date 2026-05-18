namespace AQ.StateMachine.Entities;

/// <summary>
/// Metadata stored on a Timer-type trigger defining when it should fire.
/// Serialized as JSON in StateMachineTrigger.TriggerMetadataJson.
/// </summary>
public record TimerTriggerMetadata(int AfterDays);
