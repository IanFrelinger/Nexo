namespace Nexo.Core.Application.Execution.Routing;

/// <summary>
/// Lightweight error payload for Result/Error control-flow.
/// </summary>
public sealed record Error
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Detail { get; init; }
}

/// <summary>
/// Unit value for successful operations that return no payload.
/// </summary>
public readonly struct Unit
{
    public static Unit Value => default;
}

/// <summary>
/// Success/failure wrapper that avoids exception-based control-flow.
/// </summary>
public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T? Value { get; }

    public Error? Error { get; }

    public static Result<T> Success(T value)
        => new(true, value, null);

    public static Result<T> Failure(string code, string message, string? detail = null)
        => new(false, default, new Error
        {
            Code = code,
            Message = message,
            Detail = detail
        });
}
