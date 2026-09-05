namespace Ashlar.Contracts.Distributed;

/// <summary>
/// Where an <see cref="ExecutionEnvelope"/> may be fulfilled. The wire contract is
/// the same for every target; ownership of the worker differs, not trust semantics.
/// </summary>
public enum ExecutionTarget
{
    /// <summary>Execute on the issuing node (edge or workstation).</summary>
    Local = 0,

    /// <summary>Execute on a directly addressed peer that shares the same trust domain.</summary>
    Peer = 1,

    /// <summary>Execute on a cluster scheduler (self-hosted or managed).</summary>
    Cluster = 2
}
