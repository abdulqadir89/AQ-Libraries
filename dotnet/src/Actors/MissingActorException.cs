namespace AQ.Actors;

/// <summary>Thrown when a write needs an actor but none is active.</summary>
public sealed class MissingActorException : InvalidOperationException
{
    public MissingActorException()
        : base("No actor is active. Every audited write must run under an authenticated principal; " +
               "use IActorContext.Begin or IActorScopeFactory for background work.")
    {
    }

    public MissingActorException(string message) : base(message)
    {
    }
}
