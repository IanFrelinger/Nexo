namespace Nexo.CLI.Unity.Pipeline;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Animator and state-machine rules for generated gameplay code.</summary>
public sealed record AnimationConstraints
{
    /// <summary>Maximum blend transition duration in seconds.</summary>
    public double MaxTransitionDuration { get; init; } = 0.3;

    /// <summary>When true, transitions must specify exit time before blending.</summary>
    public bool RequireExitTime { get; init; }
}
