using System.Diagnostics.CodeAnalysis;

namespace AQ.Actors;

/// <summary><see cref="IActorContext"/> backed by an <see cref="AsyncLocal{T}"/> stack.</summary>
public sealed class ActorContext : IActorContext
{
    private readonly AsyncLocal<Scope?> _scope = new();

    // RULE ACT-2
    public Actor Current => TryGet(out var actor) ? actor : throw new MissingActorException();

    public bool TryGet([NotNullWhen(true)] out Actor? actor)
    {
        actor = Active(_scope.Value)?.Actor;
        return actor is not null;
    }

    // RULE ACT-3
    public IDisposable Begin(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var scope = new Scope(this, actor, Active(_scope.Value));
        _scope.Value = scope;
        return scope;
    }

    private static Scope? Active(Scope? scope)
    {
        while (scope is { Disposed: true })
        {
            scope = scope.Parent;
        }

        return scope;
    }

    private sealed class Scope(ActorContext owner, Actor actor, Scope? parent) : IDisposable
    {
        public Actor Actor { get; } = actor;
        public Scope? Parent { get; } = parent;
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            // Out-of-order dispose just marks the scope; it is skipped once reached.
            Disposed = true;
            if (ReferenceEquals(owner._scope.Value, this))
            {
                owner._scope.Value = Active(Parent);
            }
        }
    }
}
