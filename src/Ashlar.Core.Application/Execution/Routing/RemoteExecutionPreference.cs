using Ashlar.Core.Domain;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Execution.Routing;

/// <summary>
/// Caller-specified preference for where a job should execute when the local
/// node cannot satisfy <see cref="JobRequirements"/>.
/// <para>
/// <b>Routing model:</b> The <see cref="ICapabilityRouter"/> evaluates targets
/// in this order: local NCR → peer network → RunPod cloud.
/// <c>RemoteExecutionPreference</c> lets callers constrain or override that
/// default cascade.
/// </para>
/// </summary>
public enum RemoteExecutionPreference
{
    /// <summary>Follow the system-default routing cascade (local → peer → cloud).</summary>
    UseSystemDefault = 0,
    /// <summary>Skip peer network; go directly to RunPod cloud if local fails.</summary>
    CloudOnly = 1,
    /// <summary>Try the peer network first; fall back to cloud if no peer can serve.</summary>
    PreferPeerNetwork = 2,
    /// <summary>Only use the peer network; fail if no peer is available.</summary>
    PeerNetworkOnly = 3
}
