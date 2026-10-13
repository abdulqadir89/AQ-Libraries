using System.Diagnostics.CodeAnalysis;

namespace AQ.Actors;

/// <summary>Ambient "who is doing this now". Register as a singleton; state flows with the async context.</summary>
public interface IActorContext
{
    /// <summary>The active actor. Throws <see cref="MissingActorException"/> when none.</summary>
    Actor Current { get; }

    bool TryGet([NotNullWhen(true)] out Actor? actor);

    /// <summary>Makes <paramref name="actor"/> current until the result is disposed, which restores the previous actor.</summary>
    IDisposable Begin(Actor actor);
}
