namespace AQ.CQRS.Command;

/// <summary>
/// Marker interface for commands that return a result.
/// </summary>
public interface ICommand { }

/// <summary>
/// Interface for commands that return a specific result type.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
public interface ICommand<TResult> : ICommand { }
