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
