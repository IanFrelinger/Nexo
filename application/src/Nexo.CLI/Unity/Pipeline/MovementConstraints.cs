namespace Nexo.CLI.Unity.Pipeline;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Character movement limits for generated controller code.</summary>
public sealed record MovementConstraints
{
    /// <summary>Maximum movement speed in world units per second.</summary>
    public double MaxSpeed { get; init; }

    /// <summary>When true, movement logic must include grounded checks.</summary>
    public bool RequireGroundCheck { get; init; }

    /// <summary>When true, jump logic must implement coyote-time grace.</summary>
    public bool RequireCoyoteTime { get; init; }
}
