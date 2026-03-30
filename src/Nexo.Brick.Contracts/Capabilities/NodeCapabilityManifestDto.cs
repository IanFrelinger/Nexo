namespace Nexo.BrickContracts.Capabilities;

public enum NodeTierDto
{
    Nano,
    Micro,
    Standard,
    Core
}

public enum PlatformTypeDto
{
    Windows,
    macOS,
    Linux,
    iOS,
    Android,
    Unknown
}

public enum TaskCapabilityDto
{
    TextGeneration,
    CodeGeneration,
    Embeddings,
    Vision,
    Reasoning,
    Classification
}

/// <summary>
/// Capability manifest published by each node to assist routing decisions.
/// </summary>
public sealed class NodeCapabilityManifestDto
{
    public string NodeId { get; set; } = string.Empty;
    public NodeTierDto Tier { get; set; } = NodeTierDto.Nano;
    public PlatformTypeDto Platform { get; set; } = PlatformTypeDto.Unknown;
    public string[] HotModelIds { get; set; } = [];
    public string[] AvailableModelIds { get; set; } = [];
    public TaskCapabilityDto[] SupportedCapabilities { get; set; } = [];
    public bool AcceptingRemoteWork { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}
