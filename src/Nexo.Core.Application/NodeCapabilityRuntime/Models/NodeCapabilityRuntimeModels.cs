namespace Nexo.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Capability tier of the current node.
/// </summary>
public enum NodeTier
{
    Nano,
    Micro,
    Standard,
    Core
}

/// <summary>
/// Host platform of the current runtime.
/// </summary>
public enum PlatformType
{
    Windows,
    macOS,
    Linux,
    iOS,
    Android,
    Unknown
}

/// <summary>
/// Task capability types used for model suitability and routing.
/// </summary>
public enum TaskCapability
{
    TextGeneration,
    CodeGeneration,
    Embeddings,
    Vision,
    Reasoning,
    Classification
}

public enum QualityRequirement
{
    Low,
    Medium,
    High,
    Maximum
}

public enum LatencyRequirement
{
    RealTime,
    Interactive,
    Background,
    Overnight
}

public enum PrivacyBoundary
{
    Standard,
    LocalOnly
}

public enum ModelState
{
    Hot,
    Warm,
    Cold
}

public enum BackendType
{
    Ollama,
    LlamaCppMobile,
    OnnxRuntime
}

public enum InferenceTarget
{
    Local,
    Escalate
}

public enum ResolutionReason
{
    BestLocalFit,
    EscalatedInsufficientMemory,
    EscalatedQualityRequirement,
    EscalatedPolicyBlocked,
    ForcedLocal
}

public enum AppState
{
    Foreground,
    Background,
    BackgroundRestricted,
    Suspended
}

public enum ThermalState
{
    Nominal,
    Fair,
    Serious,
    Critical,
    Severe
}

public sealed record NetworkLatencyProfile
{
    public bool IsWifi { get; init; }
    public bool IsMetered { get; init; }
    public int AverageLatencyMs { get; init; }
}

public sealed record StorageProfile
{
    public long AvailableBytes { get; init; }
    public long TotalBytes { get; init; }
}

public sealed record NodeProfile
{
    public long AvailableVRAMBytes { get; init; }
    public long TotalVRAMBytes { get; init; }
    public long AvailableRAMBytes { get; init; }
    public long TotalRAMBytes { get; init; }
    public float GPUUtilizationPercent { get; init; }
    public float CPUUtilizationPercent { get; init; }
    public bool IsOnBattery { get; init; }
    public bool IsCharging { get; init; }
    public float BatteryLevelPercent { get; init; } = 100f;
    public bool IsUserActive { get; init; }
    public AppState AppState { get; init; } = AppState.Foreground;
    public ThermalState ThermalState { get; init; } = ThermalState.Nominal;
    public bool HasBackgroundTaskPermission { get; init; }
    public bool IsBatteryOptimizationEnabled { get; init; }
    public NetworkLatencyProfile NetworkLatency { get; init; } = new();
    public StorageProfile Storage { get; init; } = new();
    public NodeTier Tier { get; init; } = NodeTier.Nano;
    public PlatformType Platform { get; init; } = PlatformType.Unknown;
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record TaskContext
{
    public TaskCapability RequiredCapability { get; init; }
    public QualityRequirement Quality { get; init; } = QualityRequirement.Medium;
    public LatencyRequirement Latency { get; init; } = LatencyRequirement.Background;
    public PrivacyBoundary Privacy { get; init; } = PrivacyBoundary.Standard;
    public bool IsUserFacing { get; init; }
    public bool IsDelegationTask { get; init; }
    public string? RequestedProvider { get; init; }
}

public sealed record ModelDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Provider { get; init; }
    public string? ProviderModelId { get; init; }
    public ModelState State { get; init; } = ModelState.Cold;
    public float QualityScore { get; init; }
    public float SpeedScore { get; init; }
    public long WeightSizeBytes { get; init; }
    public long MinVRAMRequiredBytes { get; init; }
    public long MinRAMRequiredBytes { get; init; }
    public bool RequiresRemote { get; init; }
    public IReadOnlySet<TaskCapability> Capabilities { get; init; } = new HashSet<TaskCapability>();
    public IReadOnlySet<PlatformType> SupportedPlatforms { get; init; } = new HashSet<PlatformType>();
}

public sealed record ModelResolution
{
    public ModelDescriptor Model { get; init; } = new();
    public InferenceTarget Target { get; init; }
    public ResolutionReason Reason { get; init; }
}

public sealed record NodeCapabilityManifest
{
    public string NodeId { get; init; } = string.Empty;
    public NodeTier Tier { get; init; }
    public PlatformType Platform { get; init; } = PlatformType.Unknown;
    public string[] HotModelIds { get; init; } = [];
    public string[] AvailableModelIds { get; init; } = [];
    public TaskCapability[] SupportedCapabilities { get; init; } = [];
    public bool AcceptingRemoteWork { get; init; }
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record ConstraintUpdate
{
    public NodeProfile Previous { get; init; } = new();
    public NodeProfile Current { get; init; } = new();
    public string Reason { get; init; } = "ProfileUpdated";
}

public sealed record PullProgress
{
    public string ModelId { get; init; } = string.Empty;
    public long BytesDownloaded { get; init; }
    public long TotalBytes { get; init; }
}

public sealed record InferenceRequest
{
    public string ModelId { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public string? SystemPrompt { get; init; }
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
}

public sealed record InferenceResult
{
    public string Output { get; init; } = string.Empty;
    public int TokenCount { get; init; }
    public TimeSpan Duration { get; init; }
}
