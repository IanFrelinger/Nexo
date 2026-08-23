namespace Ashlar.Infrastructure.Execution.Ollama;

/// <summary>Success/failure result wrapper for Ollama provider calls.</summary>
/// <typeparam name="T">Success value type.</typeparam>
public sealed record Result<T>
{
    private Result(T? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>Value.</summary>
    public T? Value { get; }

    /// <summary>Error.</summary>
    public Error? Error { get; }

    /// <summary>Is success.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>Creates a successful result.</summary>
    public static Result<T> Success(T value) => new(value, null);

    /// <summary>Creates a failed result.</summary>
    public static Result<T> Failure(Error error) => new(default, error);
}
