namespace Nexo.Infrastructure.Execution.Ollama;

public sealed record Error(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record Result<T>
{
    private Result(T? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value, null);

    public static Result<T> Failure(Error error) => new(default, error);
}
