namespace AQ.CQRS.Command;

/// <summary>
/// Base command for core application commands (no return value)
/// </summary>
public abstract record BaseCommand : ICommand
{
    // Add common properties for core commands here
}

/// <summary>
/// Base command for core application commands with return value
/// </summary>
/// <typeparam name="TResult">The type of the command result</typeparam>
public abstract record BaseCommand<TResult> : ICommand<TResult>
{
    // Add common properties for core commands here
}
