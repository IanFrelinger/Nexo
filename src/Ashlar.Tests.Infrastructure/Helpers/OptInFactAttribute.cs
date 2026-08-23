using Xunit;

namespace Ashlar.Tests.Infrastructure.Helpers;

/// <summary>
/// A <see cref="FactAttribute"/> for tests that need an external dependency (Docker, Ollama, Mapbox, ...)
/// and are opted into with an environment variable. When the variable is not truthy (<c>1</c> / <c>true</c>),
/// or any <see cref="RequiredEnvironmentVariables"/> entry is empty, the test is reported as <b>Skipped</b>
/// with a reason naming the variable — instead of returning early and counting as <b>Passed</b>, which is
/// what the old <c>if (!enabled) return;</c> idiom did and what made <c>dotnet test</c> look green with the
/// dependency absent.
/// </summary>
/// <remarks>
/// xunit 2.x has no dynamic skip, so the condition is evaluated at discovery time (which is where
/// environment variables are stable anyway). Runtime conditions that can only be known once the test
/// runs (a fixture that failed to start after opt-in) are still guarded inside the test.
/// <c>Timeout</c> and <c>DisplayName</c> work exactly as on <c>[Fact]</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class OptInFactAttribute : FactAttribute
{
    private string[] _requiredEnvironmentVariables = Array.Empty<string>();

    /// <summary>
    /// Creates a fact that runs only when <paramref name="environmentVariable"/> is <c>1</c> or <c>true</c>.
    /// </summary>
    /// <param name="environmentVariable">Opt-in switch, for example <c>ASHLAR_TEST_REAL_VISION</c>.</param>
    /// <param name="dependency">Human-readable dependency name used in the skip reason (for example "Real Ollama vision model").</param>
    public OptInFactAttribute(string environmentVariable, string dependency)
    {
        EnvironmentVariable = environmentVariable;
        Dependency = dependency;
        if (!IsTruthy(Environment.GetEnvironmentVariable(environmentVariable)))
        {
            Skip = $"{dependency}: set {environmentVariable}=1 to run.";
        }
    }

    /// <summary>Opt-in environment variable checked at discovery time.</summary>
    public string EnvironmentVariable { get; }

    /// <summary>Dependency name used in the skip reason.</summary>
    public string Dependency { get; }

    /// <summary>
    /// Additional environment variables that must be non-empty for the test to run (for example an access
    /// token). Evaluated only when <see cref="EnvironmentVariable"/> is truthy so the primary reason wins.
    /// </summary>
    public string[] RequiredEnvironmentVariables
    {
        get => _requiredEnvironmentVariables;
        set
        {
            _requiredEnvironmentVariables = value ?? Array.Empty<string>();
            if (Skip is not null)
            {
                return;
            }

            var missing = _requiredEnvironmentVariables
                .Where(name => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
                .ToArray();
            if (missing.Length > 0)
            {
                Skip = $"{Dependency}: {EnvironmentVariable}=1 is set but {string.Join(", ", missing)} is empty.";
            }
        }
    }

    /// <summary>Shared truthy parse: <c>1</c> or <c>true</c> (case-insensitive).</summary>
    /// <param name="value">Raw environment value.</param>
    /// <returns>Whether the value opts in.</returns>
    public static bool IsTruthy(string? value) =>
        string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}
