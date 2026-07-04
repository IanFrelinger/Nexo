namespace Nexo.Core.Application.Execution.Routing;

/// <summary>
/// Success/failure wrapper that avoids exception-based control-flow.
/// </summary>
/// <typeparam name="T">Type of the success payload.</typeparam>
public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>Whether the operation completed successfully.</summary>
    public bool IsSuccess { get; }

    /// <summary>Whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Success payload; undefined when <see cref="IsFailure"/> is true.</summary>
    public T? Value { get; }

    /// <summary>Error details; undefined when <see cref="IsSuccess"/> is true.</summary>
    public Error? Error { get; }

    /// <summary>Creates a successful result wrapping the given value.</summary>
    /// <param name="value">Success payload.</param>
    public static Result<T> Success(T value)
        => new(true, value, null);

    /// <summary>Creates a failed result with the given error details.</summary>
    /// <param name="code">Machine-readable error code.</param>
    /// <param name="message">Human-readable error message.</param>
    /// <param name="detail">Optional additional diagnostic detail.</param>
    public static Result<T> Failure(string code, string message, string? detail = null)
        => new(false, default, new Error
        {
            Code = code,
            Message = message,
            Detail = detail
        });
}
