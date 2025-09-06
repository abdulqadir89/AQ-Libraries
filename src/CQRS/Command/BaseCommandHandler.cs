using AQ.Common.Domain.Results;

namespace AQ.Common.Application.CQRS;

/// <summary>
/// Base class for command handlers that provides common validation and error handling patterns.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
public abstract class BaseCommandHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Correlation ID for tracking this command execution across audit logs
    /// </summary>
    protected Guid CorrelationId { get; } = Guid.CreateVersion7();

    /// <summary>
    /// Handles the command and returns a result.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation result.</returns>
    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = await ValidateAsync(command, cancellationToken);
            if (validationResult.IsFailure)
                return validationResult;

            return await HandleAsync(command, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log the exception and re-throw as exceptions represent infrastructure/technical errors
            // Business validation errors should be returned as Result.Failure
            throw new InvalidOperationException($"Error handling command {typeof(TCommand).Name}", ex);
        }
    }

    /// <summary>
    /// Validates the command before handling.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the validation result.</returns>
    protected virtual Task<Result> ValidateAsync(TCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Handles the validated command.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation result.</returns>
    protected abstract Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Base class for command handlers with result that provides common validation and error handling patterns.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public abstract class BaseCommandHandler<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>
    /// Correlation ID for tracking this command execution across audit logs
    /// </summary>
    protected Guid CorrelationId { get; } = Guid.CreateVersion7();
    /// <summary>
    /// Handles the command and returns a result.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation result.</returns>
    public async Task<Result<TResult>> Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = await ValidateAsync(command, cancellationToken);
            if (validationResult.IsFailure)
                return Result.Failure<TResult>(validationResult.Error);

            return await HandleAsync(command, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log the exception and re-throw as exceptions represent infrastructure/technical errors
            // Business validation errors should be returned as Result.Failure
            throw new InvalidOperationException($"Error handling command {typeof(TCommand).Name}", ex);
        }
    }

    /// <summary>
    /// Validates the command before handling.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the validation result.</returns>
    protected virtual Task<Result> ValidateAsync(TCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Handles the validated command.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation result.</returns>
    protected abstract Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
