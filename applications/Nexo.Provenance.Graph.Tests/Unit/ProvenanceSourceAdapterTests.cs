using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Provenance.Graph.Ingestion;
using Nexo.Provenance.Graph.InMemory;
using Nexo.Provenance.Graph.Sources;
using Xunit;

namespace Nexo.Provenance.Graph.Tests.Unit;

public sealed class ProvenanceSourceAdapterTests
{
    [Fact]
    public async Task SignedRepositorySample_ProjectsAndQueriesThroughSourceOwnedAuthority()
    {
        var sampleDirectory = Path.Combine(AppContext.BaseDirectory, "provenance-graph");
        var source = new PhysicalAtomDirectorySourceAdapter(sampleDirectory);
        var authority = new SourceProvenanceChainHeadAuthority(source);
        var store = new InMemoryProvenanceGraphStore();
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);

        var bundles = await source.ReadAsync();
        var report = await projector.ProjectAsync(bundles);
        var queries = new InMemoryProvenanceGraphQueries(store, authority);
        var result = await queries.ArtifactsUnderPolicyAsync(
            "SelfProducedBrickCertificationPolicy",
            "1.0.0");

        report.AcceptedCount.Should().Be(1);
        report.Rejections.Should().BeEmpty();
        result.ChainHeadHash.Should().Be(report.ChainHeadHash);
        result.ArtifactIds.Should().Equal(
            "8f168a714d1b9833b60055ba3d3b0da110198c5672a1ec73e4baf52c126a02e6");
    }
}
