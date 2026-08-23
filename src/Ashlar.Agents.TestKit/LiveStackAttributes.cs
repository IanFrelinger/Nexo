using Xunit;

namespace Ashlar.Agents.TestKit;

/// <summary>
/// Opt-in switches for tests that cannot run offline. Everything else in the agent
/// suites must pass with the model and container edges absent — that is the whole
/// point of the TestKit.
/// </summary>
public static class LiveEnvironment
{
    /// <summary>Enables tests needing a container engine / local service stack.</summary>
    public const string LiveStackVariable = "ASHLAR_E2E_LIVE_STACK";

    /// <summary>Enables tests needing a reachable local model.</summary>
    public const string LiveModelVariable = "ASHLAR_E2E_LIVE_MODEL";

    /// <summary>True when live-stack tests are opted in.</summary>
    public static bool StackEnabled => IsSet(LiveStackVariable);

    /// <summary>
    /// True when live-model tests are opted in. <c>SKIP_OLLAMA=1</c> forces this off,
    /// so the offline knob wins over a stale opt-in.
    /// </summary>
    public static bool ModelEnabled =>
        IsSet(LiveModelVariable)
        && !string.Equals(
            Environment.GetEnvironmentVariable(OfflineAgentEnvironment.SkipModel),
            "1",
            StringComparison.Ordinal);

    private static bool IsSet(string name) =>
        string.Equals(Environment.GetEnvironmentVariable(name), "1", StringComparison.Ordinal);
}

/// <summary>Fact requiring a live container/service stack.</summary>
public sealed class LiveStackFactAttribute : FactAttribute
{
    /// <summary>Creates the attribute, skipping unless the stack is opted in.</summary>
    public LiveStackFactAttribute()
    {
        if (!LiveEnvironment.StackEnabled)
            Skip = $"requires a live local stack; set {LiveEnvironment.LiveStackVariable}=1 to run";
    }
}

/// <summary>Theory requiring a live container/service stack.</summary>
public sealed class LiveStackTheoryAttribute : TheoryAttribute
{
    /// <summary>Creates the attribute, skipping unless the stack is opted in.</summary>
    public LiveStackTheoryAttribute()
    {
        if (!LiveEnvironment.StackEnabled)
            Skip = $"requires a live local stack; set {LiveEnvironment.LiveStackVariable}=1 to run";
    }
}

/// <summary>Fact requiring a reachable local model.</summary>
public sealed class LiveModelFactAttribute : FactAttribute
{
    /// <summary>Creates the attribute, skipping unless a live model is opted in.</summary>
    public LiveModelFactAttribute()
    {
        if (!LiveEnvironment.ModelEnabled)
            Skip = $"requires a reachable local model; set {LiveEnvironment.LiveModelVariable}=1 (and clear {OfflineAgentEnvironment.SkipModel}) to run";
    }
}

/// <summary>Theory requiring a reachable local model.</summary>
public sealed class LiveModelTheoryAttribute : TheoryAttribute
{
    /// <summary>Creates the attribute, skipping unless a live model is opted in.</summary>
    public LiveModelTheoryAttribute()
    {
        if (!LiveEnvironment.ModelEnabled)
            Skip = $"requires a reachable local model; set {LiveEnvironment.LiveModelVariable}=1 (and clear {OfflineAgentEnvironment.SkipModel}) to run";
    }
}
