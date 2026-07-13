using Nexo.Provenance.Graph.Models;
using Nexo.Provenance.Graph.Ports;

namespace Nexo.Provenance.Graph.Null;

/// <summary>No-op query implementation paired with <see cref="NullProvenanceGraphStore"/>.</summary>
public sealed class NullProvenanceGraphQueries : IProvenanceGraphQueries
{
    public Task<LineageQueryResult> LineageOfAsync(string artifactId, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Provenance graph is not enabled.");

    public Task<ArtifactsUnderPolicyResult> ArtifactsUnderPolicyAsync(string policyId, string policyVersion, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Provenance graph is not enabled.");

    public Task<BlastRadiusResult> BlastRadiusOfAsync(string policyId, string policyVersion, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Provenance graph is not enabled.");
}
