using Nexo.Core.Domain;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Execution.Routing;

/// <summary>
/// Marker interface for executors that forward jobs to a peer node in the
/// mesh network.  Trust policy (<c>RunPodBrickConfig.PeerTrustPolicy</c>)
/// is enforced before dispatch.
/// </summary>
public interface IPeerExecutor : IBrickExecutor
{
}
