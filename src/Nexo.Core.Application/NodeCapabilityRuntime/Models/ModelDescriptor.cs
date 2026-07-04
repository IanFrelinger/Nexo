namespace Nexo.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Describes a model available for selection, loading, and inference.
/// </summary>
public sealed record ModelDescriptor
{
    /// <summary>Local identifier for this model within the node catalog.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Upstream provider name (Ollama, OpenAI, etc.), if applicable.</summary>
    public string? Provider { get; init; }

    /// <summary>Provider-specific model identifier used for pull and inference.</summary>
    public string? ProviderModelId { get; init; }

    /// <summary>Current load state in the local model cache.</summary>
    public ModelState State { get; init; } = ModelState.Cold;

    /// <summary>Relative quality score used for model ranking (higher is better).</summary>
    public float QualityScore { get; init; }

    /// <summary>Relative speed score used for model ranking (higher is faster).</summary>
    public float SpeedScore { get; init; }

    /// <summary>On-disk size of model weights in bytes.</summary>
    public long WeightSizeBytes { get; init; }

    /// <summary>Minimum VRAM required to load and run this model.</summary>
    public long MinVRAMRequiredBytes { get; init; }

    /// <summary>Minimum system RAM required to load and run this model.</summary>
    public long MinRAMRequiredBytes { get; init; }

    /// <summary>When true, inference must run on a remote node rather than locally.</summary>
    public bool RequiresRemote { get; init; }

    /// <summary>Task capabilities this model supports.</summary>
    public IReadOnlyCollection<TaskCapability> Capabilities { get; init; } = new HashSet<TaskCapability>();

    /// <summary>Platforms on which this model can run.</summary>
    public IReadOnlyCollection<PlatformType> SupportedPlatforms { get; init; } = new HashSet<PlatformType>();
}
