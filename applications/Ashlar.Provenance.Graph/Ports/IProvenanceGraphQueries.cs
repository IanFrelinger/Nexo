using Ashlar.Provenance.Graph.Models;

namespace Ashlar.Provenance.Graph.Ports;

/// <summary>Typed query API over the provenance graph projection.</summary>
public interface IProvenanceGraphQueries
{
    /// <summary>Full upstream cert chain and dependencies for an artifact.</summary>
    Task<LineageQueryResult> LineageOfAsync(string artifactId, CancellationToken cancellationToken = default);

    /// <summary>Artifacts whose cert chain passes through a policy version.</summary>
    Task<ArtifactsUnderPolicyResult> ArtifactsUnderPolicyAsync(string policyId, string policyVersion, CancellationToken cancellationToken = default);

    /// <summary>Downstream artifacts affected if a policy version were revoked.</summary>
    Task<BlastRadiusResult> BlastRadiusOfAsync(string policyId, string policyVersion, CancellationToken cancellationToken = default);
}
