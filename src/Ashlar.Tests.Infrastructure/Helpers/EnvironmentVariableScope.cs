namespace Ashlar.Tests.Infrastructure.Helpers;

/// <summary>
/// Restores an environment variable when disposed. Used so default-behavior tests
/// are not affected by CI workflow env (e.g. <c>ASHLAR_STRICT_MODE=1</c>).
/// </summary>
public sealed class EnvironmentVariableScope : IDisposable
{
    private readonly string _key;
    private readonly string? _previous;

    public EnvironmentVariableScope(string key, string? value)
    {
        _key = key;
        _previous = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, value);
    }

    public static EnvironmentVariableScope Unset(string key) => new(key, null);

    public void Dispose() => Environment.SetEnvironmentVariable(_key, _previous);
}
