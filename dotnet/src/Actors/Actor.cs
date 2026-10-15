namespace AQ.Actors;

/// <summary>The kind of principal behind an <see cref="Actor"/>.</summary>
public enum PrincipalKind
{
    /// <summary>A signed-in person.</summary>
    Human = 1,

    /// <summary>A registered machine client (client_credentials).</summary>
    Service = 2,

    /// <summary>Platform-owned work: background jobs, event dispatch, deployment.</summary>
    System = 3
}

/// <summary>
/// Who is performing the current operation. Every write is attributed to an actor.
/// </summary>
/// <param name="PrincipalId">Stable id of the principal doing the work.</param>
/// <param name="Kind">Kind of principal.</param>
/// <param name="OnBehalfOfId">Principal this work is done for, when delegated (e.g. an import job run for a user).</param>
/// <param name="ClientId">OAuth client id, when the actor came from a token.</param>
/// <param name="CorrelationId">Trace id tying related writes together.</param>
// RULE ACT-1
public sealed record Actor(
    Guid PrincipalId,
    PrincipalKind Kind,
    Guid? OnBehalfOfId = null,
    string? ClientId = null,
    string? CorrelationId = null);
