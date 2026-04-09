using Nexo.Core.Application.Execution.Routing;

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
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;
}
