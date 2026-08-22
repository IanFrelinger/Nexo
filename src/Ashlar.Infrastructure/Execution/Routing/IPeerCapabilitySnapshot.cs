using Ashlar.Core.Application.Execution.Routing;
using Ashlar.Core.Application.Mesh.Models;

namespace Ashlar.Infrastructure.Execution.Routing;

/// <summary>
/// Snapshot of discoverable Ashlar peers that can accept compute work.
/// </summary>
public interface IPeerCapabilitySnapshot
{
    IReadOnlyList<PeerExecutionCandidate> Candidates { get; }
}
