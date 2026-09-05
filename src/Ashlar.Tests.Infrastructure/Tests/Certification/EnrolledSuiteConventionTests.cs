using FluentAssertions;
using Ashlar.Core.Application.Paths;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Keeps the remaining formerly-UNOWNED suites (except the brick template)
/// named by a real counted runner, not a dated debt row.
/// </summary>
[Trait("Category", "Certification")]
public sealed class EnrolledSuiteConventionTests
{
    [Fact]
    public void OwnershipRegistry_NamesCompositionMeshTierC_ForMeshDirectorTests()
    {
        var columns = OwnershipRow("Ashlar.Commercial.Tests.MeshDirector.csproj");
        columns[1].Should().Be("composition-mesh-gate-tier-c");
        columns[2].Should().Be("-");
    }

    [Fact]
    public void OwnershipRegistry_NamesCertGate_ForAnalyzerTests()
    {
        var columns = OwnershipRow("Ashlar.Analyzers.Tests.csproj");
        columns[1].Should().Be("cert-gate");
        columns[2].Should().Be("-");
    }

    [Fact]
    public void OwnershipRegistry_NamesIngressUnitGate_ForAwsSnsAndDynamoDbTests()
    {
        var sns = OwnershipRow("Ashlar.Ingress.AwsSns.Tests.csproj");
        sns[1].Should().Be("ingress-unit-gate");
        sns[2].Should().Be("-");

        var dynamo = OwnershipRow("Ashlar.Ingress.DynamoDb.Tests.csproj");
        dynamo[1].Should().Be("ingress-unit-gate");
        dynamo[2].Should().Be("-");
    }

    [Fact]
    public void MeshTierC_RunsCountedMeshDirectorSuite()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/composition-mesh-gate-tier-c.sh"));
        text.Should().Contain("Ashlar.Commercial.Tests.MeshDirector");
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 4");
    }

    [Fact]
    public void CertGate_RunsCountedAnalyzerSuite()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/run-cert-gate.sh"));
        text.Should().Contain("Ashlar.Analyzers.Tests");
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 56");
    }

    [Fact]
    public void CompositionMeshGateWorkflow_RunsOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/composition-mesh-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("github.event_name == 'pull_request'");
        text.Should().NotContain("github.event_name != 'workflow_dispatch'");
    }

    [Fact]
    public void IngressUnitGate_RunsCountedAwsSnsAndDynamoDbSuites()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/ingress-unit-gate.sh"));
        text.Should().Contain("Ashlar.Ingress.AwsSns.Tests");
        text.Should().Contain("Ashlar.Ingress.DynamoDb.Tests");
        text.Should().Contain("--min-tests 11");
        text.Should().Contain("--min-tests 2");
    }

    private static string[] OwnershipRow(string csprojLeaf)
    {
        var row = File.ReadAllLines(Path.Combine(RepoPathResolver.FindRepoRoot(), "ci/test-ownership.tsv"))
            .Single(line => line.Contains(csprojLeaf, StringComparison.Ordinal));
        var columns = row.Split('\t');
        columns.Should().HaveCountGreaterThanOrEqualTo(3);
        return columns;
    }
}
