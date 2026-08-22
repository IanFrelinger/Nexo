using Ashlar.Core.Application.Execution.Routing;
using Ashlar.Core.Application.Mesh.Models;

namespace Ashlar.Infrastructure.Execution.Routing;

/// <summary>Peer execution candidate.</summary>
public sealed record PeerExecutionCandidate
{
    /// <summary>Peer id.</summary>
    public string PeerId { get; init; } = string.Empty;
    /// <summary>Endpoint.</summary>
    public string Endpoint { get; init; } = string.Empty;
    /// <summary>Available vram bytes.</summary>
    public long AvailableVramBytes { get; init; }
    /// <summary>Compute class.</summary>
    public GpuComputeClass ComputeClass { get; init; } = GpuComputeClass.None;
    /// <summary>Queue depth.</summary>
    public int QueueDepth { get; init; }
    /// <summary>Trust tier.</summary>
    public PeerTrustTier TrustTier { get; init; } = PeerTrustTier.Unknown;
    /// <summary>Captured at.</summary>
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;
}
