using FluentAssertions;
using Ashlar.Core.Application.Paths;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The motivating ownership incident: <c>Ashlar.Commercial.Tests.Fleet.Host</c>
/// was in no solution and named by no gate. These facts keep the host graph
/// in <c>Ashlar.sln</c> and the counted runner in Tier C.
/// </summary>
[Trait("Category", "Certification")]
public sealed class FleetHostEnrollmentConventionTests
{
    [Fact]
    public void AshlarSln_EnrollsFleetHostGraph()
    {
        var sln = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(), "Ashlar.sln"));
        sln.Should().Contain("Ashlar.Commercial.Fleet.Api");
        sln.Should().Contain("Ashlar.Commercial.Fleet.Host");
        sln.Should().Contain("Ashlar.Commercial.Tests.Fleet.Host");
    }

    [Fact]
    public void OwnershipRegistry_NamesCompositionMeshTierC_ForFleetHostTests()
    {
        var row = File.ReadAllLines(Path.Combine(RepoPathResolver.FindRepoRoot(), "ci/test-ownership.tsv"))
            .Single(line => line.Contains("Ashlar.Commercial.Tests.Fleet.Host.csproj", StringComparison.Ordinal));
        var columns = row.Split('\t');
        columns.Should().HaveCountGreaterThanOrEqualTo(3);
        columns[1].Should().Be("composition-mesh-gate-tier-c");
        columns[2].Should().Be("-");
    }
}
