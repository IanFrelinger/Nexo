namespace Ashlar.CLI.Unity.Pipeline;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Audio variation and spatialization rules for generated content.</summary>
public sealed record AudioConstraints
{
    /// <summary>When true, each audio event must ship multiple clip variations.</summary>
    public bool RequireVariations { get; init; }

    /// <summary>Minimum clip variations required per audio event.</summary>
    public int MinVariationsPerEvent { get; init; } = 2;

    /// <summary>When true, generated audio sources must use spatial blend.</summary>
    public bool RequireSpatialBlend { get; init; }

    /// <summary>Maximum simultaneous audio voices allowed.</summary>
    public int MaxSimultaneous { get; init; } = 32;
}
