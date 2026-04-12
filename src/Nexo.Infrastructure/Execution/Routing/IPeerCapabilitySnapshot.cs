using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Application.Mesh.Models;

namespace Nexo.Infrastructure.Execution.Routing;

/// <summary>
/// Snapshot of discoverable Nexo peers that can accept compute work.
/// </summary>
public interface IPeerCapabilitySnapshot
{
    IReadOnlyList<PeerExecutionCandidate> Candidates { get; }
}

public sealed record PeerExecutionCandidate
{
    public string PeerId { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public long AvailableVramBytes { get; init; }
    public GpuComputeClass ComputeClass { get; init; } = GpuComputeClass.None;
    public int QueueDepth { get; init; }
    public PeerTrustTier TrustTier { get; init; } = PeerTrustTier.Unknown;
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;
}
