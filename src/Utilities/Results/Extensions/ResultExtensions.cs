namespace AQ.Utilities.Results.Extensions;

/// <summary>
/// Extension methods for working with Result types.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Executes a function on the result value if the result is successful.
    /// </summary>
    /// <typeparam name="TValue">The type of the input value.</typeparam>
    /// <typeparam name="TResult">The type of the output value.</typeparam>
    /// <param name="result">The input result.</param>
    /// <param name="func">The function to execute.</param>
    /// <returns>A new result with the transformed value or the original error.</returns>
    public static Result<TResult> Map<TValue, TResult>(
        this Result<TValue> result,
        Func<TValue, TResult> func)
    {
        return result.IsFailure
            ? Result.Failure<TResult>(result.Error)
            : Result.Success(func(result.Value));
    }

    /// <summary>
    /// Executes a function on the result value if the result is successful.
    /// </summary>
    /// <typeparam name="TValue">The type of the input value.</typeparam>
    /// <typeparam name="TResult">The type of the output value.</typeparam>
    /// <param name="result">The input result.</param>
    /// <param name="func">The function to execute.</param>
    /// <returns>A new result with the transformed value or the original error.</returns>
    public static async Task<Result<TResult>> MapAsync<TValue, TResult>(
        this Result<TValue> result,
        Func<TValue, Task<TResult>> func)
    {
        return result.IsFailure
            ? Result.Failure<TResult>(result.Error)
            : Result.Success(await func(result.Value));
    }

    /// <summary>
    /// Binds the result to a function that returns another result.
    /// </summary>
    /// <typeparam name="TValue">The type of the input value.</typeparam>
    /// <typeparam name="TResult">The type of the output value.</typeparam>
    /// <param name="result">The input result.</param>
    /// <param name="func">The function to bind.</param>
    /// <returns>The result of the binding function or the original error.</returns>
    public static Result<TResult> Bind<TValue, TResult>(
        this Result<TValue> result,
        Func<TValue, Result<TResult>> func)
    {
        return result.IsFailure
            ? Result.Failure<TResult>(result.Error)
            : func(result.Value);
    }

    /// <summary>
    /// Binds the result to a function that returns another result asynchronously.
    /// </summary>
    /// <typeparam name="TValue">The type of the input value.</typeparam>
    /// <typeparam name="TResult">The type of the output value.</typeparam>
    /// <param name="result">The input result.</param>
    /// <param name="func">The function to bind.</param>
    /// <returns>The result of the binding function or the original error.</returns>
    public static async Task<Result<TResult>> BindAsync<TValue, TResult>(
        this Result<TValue> result,
        Func<TValue, Task<Result<TResult>>> func)
    {
        return result.IsFailure
            ? Result.Failure<TResult>(result.Error)
            : await func(result.Value);
    }

    /// <summary>
    /// Executes an action on the result value if the result is successful.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original result.</returns>
    public static Result<TValue> Tap<TValue>(
        this Result<TValue> result,
        Action<TValue> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Executes an action on the result value if the result is successful asynchronously.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original result.</returns>
    public static async Task<Result<TValue>> TapAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task> action)
    {
        if (result.IsSuccess)
        {
            await action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Matches the result to one of two functions based on success or failure.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The function to execute on success.</param>
    /// <param name="onFailure">The function to execute on failure.</param>
    /// <returns>The result of the matching function.</returns>
    public static TResult Match<TValue, TResult>(
        this Result<TValue> result,
        Func<TValue, TResult> onSuccess,
        Func<Error, TResult> onFailure)
    {
        return result.IsSuccess
            ? onSuccess(result.Value)
            : onFailure(result.Error);
    }

    /// <summary>
    /// Converts a Result to a Result&lt;T&gt; with the specified value.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="value">The value to include if the result is successful.</param>
    /// <returns>A Result&lt;T&gt; with the specified value or the original error.</returns>
    public static Result<TValue> WithValue<TValue>(this Result result, TValue value)
    {
        return result.IsSuccess
            ? Result.Success(value)
            : Result.Failure<TValue>(result.Error);
    }
}
