namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// Runtime preferences for selecting and falling back between implementations for a brick.
/// This is the shared, framework-wide “hot swap” configuration shape.
/// </summary>
public sealed record BrickRuntimeSpec
{
    /// <summary>
    /// Preference: "agentic", "deterministic", or "auto".
    /// </summary>
    public string Prefer { get; init; } = "auto";

    /// <summary>
    /// Ordered fallback chain when preferred implementation is unavailable or fails at runtime.
    /// </summary>
    public IReadOnlyList<ImplementationType> Fallback { get; init; } =
        new[] { ImplementationType.Deterministic, ImplementationType.Agentic };

    /// <summary>
    /// Creates a spec that prefers deterministic implementation only (no agentic fallback).
    /// </summary>
    public static BrickRuntimeSpec DeterministicOnly()
        => new() { Prefer = "deterministic", Fallback = new[] { ImplementationType.Deterministic } };

    /// <summary>
    /// Creates a spec that prefers agentic with deterministic fallback.
    /// </summary>
    public static BrickRuntimeSpec AgenticWithDeterministicFallback()
        => new() { Prefer = "agentic", Fallback = new[] { ImplementationType.Agentic, ImplementationType.Deterministic } };

    /// <summary>Agentic only; no deterministic fallback.</summary>
    public static BrickRuntimeSpec AgenticOnly()
        => new() { Prefer = "agentic", Fallback = new[] { ImplementationType.Agentic } };
}

