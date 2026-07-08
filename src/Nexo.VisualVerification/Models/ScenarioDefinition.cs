namespace Nexo.VisualVerification;

/// <summary>
/// A deterministic scenario: seed, fixed timestep, duration, and an input script.
/// Content-addressed via <see cref="ScenarioHasher"/>.
/// </summary>
public sealed record ScenarioDefinition(
    ulong Seed,
    double FixedTimestepSeconds,
    int TotalSteps,
    IReadOnlyList<ScriptedInputEvent> InputScript);
