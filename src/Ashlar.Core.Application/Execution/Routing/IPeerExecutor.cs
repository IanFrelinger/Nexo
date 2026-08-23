using Ashlar.Core.Domain;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Execution.Routing;

/// <summary>
/// Marker interface for executors that forward jobs to a peer node in the
/// mesh network.  Trust policy (<c>RunPodBrickConfig.PeerTrustPolicy</c>)
/// is enforced before dispatch.
/// </summary>
public interface IPeerExecutor : IBrickExecutor
{
}
