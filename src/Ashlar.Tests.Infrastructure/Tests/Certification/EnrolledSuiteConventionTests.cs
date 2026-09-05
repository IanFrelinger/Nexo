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
    public void OwnershipRegistry_NamesCertGate_ForContractsTests()
    {
        var columns = OwnershipRow("Ashlar.Tests.Contracts.csproj");
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
    public void MeshTierC_RunsCountedFleetSuite()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/composition-mesh-gate-tier-c.sh"));
        text.Should().Contain("Ashlar.Commercial.Tests.Fleet.csproj");
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--expected-prefix \"Ashlar.Commercial.Tests.Fleet.\"");
        text.Should().Contain("--min-tests 176");
        text.Should().NotContain("dotnet test \"$FLEET_TESTS\"");
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
    public void CompatTierA_RunsCountedFleetCheckpointAndCompositionSlices()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/compat-gate-tier-a.sh"));
        text.Should().Contain("Ashlar.Commercial.Tests.Fleet.csproj");
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 1");
        text.Should().Contain("--min-tests 4");
        text.Should().Contain("MeshTaskExecutionServiceTests.MigrateForCheckpointAsync");
        text.Should().NotContain("dotnet test \"$INFRA\"");
        text.Should().NotContain("dotnet test \"$FLEET_TESTS\"");
    }

    [Fact]
    public void CompatTierC_RunsCountedConfigurationAndKernelPhaseSlices()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/compat-gate-tier-c.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 2");
        text.Should().Contain("--min-tests 4");
        text.Should().Contain("KernelPhaseResolutionTests");
        text.Should().NotContain("dotnet test \"$INFRA\"");
    }

    [Fact]
    public void CompatGateWorkflow_RunsOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/compat-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("Ashlar.Commercial.Tests.Fleet");
        text.Should().Contain("scripts/compat-gate.sh");
    }

    [Fact]
    public void DrTierB_RunsCountedKnowledgeStoreSlice()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/dr-gate-tier-b.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 8");
        text.Should().Contain("LiteDbUserKnowledgeLogStoreTests");
        text.Should().NotContain("dotnet test \"$INFRA\"");
    }

    [Fact]
    public void DrGateWorkflow_RunsOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/dr-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("LiteDbUserKnowledgeLogStoreTests");
    }

    [Fact]
    public void PerfTierA_RunsCountedOrchAndBackgroundSlices()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/perf-gate-tier-a.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 3");
        text.Should().Contain("--min-tests 9");
        text.Should().Contain("Ashlar.Tests.Orchestration.Performance");
        text.Should().Contain("Ashlar.Tests.BackgroundAgents.Performance");
        text.Should().NotContain("dotnet test \"$ORCH\"");
        text.Should().NotContain("dotnet test \"$BG\"");
    }

    [Fact]
    public void PerfGateWorkflow_RunsTierAOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/perf-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("perf-gate-tier-a.sh");
        text.Should().Contain("github.event_name != 'pull_request'");
    }

    [Fact]
    public void CertGate_MainFilterHasCollapseFloor()
    {
        var config = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/cert-gate-config.sh"));
        var guard = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/cert-gate-zero-test-guard.sh"));
        config.Should().Contain("readonly CERT_GATE_MIN_TESTS=400");
        guard.Should().Contain("CERT_GATE_MIN_TESTS");
        guard.Should().Contain("discovery collapsed");
    }

    [Fact]
    public void CertGate_MainFilterExcludesEnrolledSuiteConventions()
    {
        var config = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/cert-gate-config.sh"));
        config.Should().Contain("FullyQualifiedName!~EnrolledSuiteConventionTests");
    }

    [Fact]
    public void CertGate_RunsCountedEnrolledSuiteConventions()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/run-cert-gate.sh"));
        text.Should().Contain("EnrolledSuiteConventionTests");
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 53");
        text.Should().Contain(
            "--expected-prefix \"Ashlar.Tests.Infrastructure.Tests.Certification.EnrolledSuiteConventionTests.\"");
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
    public void CertGate_RunsCountedContractsSuite()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/run-cert-gate.sh"));
        text.Should().Contain("Ashlar.Tests.Contracts");
        text.Should().Contain("--min-tests 18");
        text.Should().Contain("--expected-prefix \"Ashlar.Tests.Contracts.\"");
    }

    [Fact]
    public void MeshTierA_RunsCountedPipelineSuite()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/composition-mesh-gate-tier-a.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 64");
        text.Should().Contain("Ashlar.Tests.Infrastructure.Tests.Pipelines");
        text.Should().NotContain("dotnet test \"$INFRA\"");
    }

    [Fact]
    public void MeshTierB_RunsCountedCliBridgeRows()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/composition-mesh-gate-tier-b.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 3");
        text.Should().Contain("DisplayName~PipelineCommand");
        text.Should().NotContain("dotnet test \"$CLI\"");
    }

    [Fact]
    public void SecurityTierA_RunsCountedTrustSuite()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/security-gate-tier-a.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 97");
        text.Should().Contain("Ashlar.Tests.Infrastructure.Tests.Trust");
        text.Should().NotContain("dotnet test \"$INFRA\"");
    }

    [Fact]
    public void SecurityTierC_RunsCountedCliTrustSurface()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/security-gate-tier-c.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 61");
        text.Should().Contain("SafePackageReadTests");
        text.Should().NotContain("dotnet test \"$CLI_TESTS\"");
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
    public void ApplicationTierB_RunsCountedCliSuite()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/application-gate-tier-b.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("Ashlar.Tests.CLI");
        text.Should().Contain("--min-tests 200");
        text.Should().Contain("FullyQualifiedName!~UnitTestBridgeTests");
        text.Should().Contain("-f net10.0");
        text.Should().NotContain("dotnet test \"$CLI_TESTS\"");
    }

    [Fact]
    public void ApplicationTierC_RunsCountedApiSuiteOnNet10()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/application-gate-tier-c.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 4");
        text.Should().Contain("-f net10.0");
        text.Should().Contain("ApiDevelopmentHostDiTests");
        text.Should().NotContain("-f net8.0");
        text.Should().NotContain("dotnet test \"$INFRA\"");
    }

    [Fact]
    public void ApplicationGateWorkflow_RunsTierCOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/application-gate.yml"));
        text.Should().Contain("Tests/API/**");
        text.Should().Contain("application-gate-tier-c");
        text.Should().NotContain("github.event_name == 'workflow_dispatch' && inputs.tier == 'c'");
    }

    [Fact]
    public void ApplicationTierD_RefusesMissingDocker()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/application-gate-tier-d.sh"));
        text.Should().Contain("requires a working Docker daemon");
        text.Should().Contain("exit 2");
        text.Should().NotContain("skipped (no Docker)");
    }

    [Fact]
    public void MeshTierD_RefusesMissingDocker()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/composition-mesh-gate-tier-d.sh"));
        text.Should().Contain("requires a working Docker daemon");
        text.Should().Contain("exit 2");
        text.Should().NotContain("skipped (Docker not available)");
    }

    [Fact]
    public void OpsTierD_RefusesMissingDocker()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/ops-gate-tier-d.sh"));
        text.Should().Contain("requires a working Docker daemon");
        text.Should().Contain("exit 2");
        text.Should().NotContain("skipped (Docker not available)");
    }

    [Fact]
    public void OpsTierD_RefusesMissingProofFlags()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/ops-gate-tier-d.sh"));
        text.Should().Contain("OPS_GATE_MESH_DEEP=1 or OPS_GATE_CHAOS_LITE=1");
        text.Should().Contain("refusing to skip mesh resilience");
        text.Should().Contain("exit 2");
        text.Should().NotContain("Tier D: skipped");
    }

    [Fact]
    public void OpsGateFull_SkipsTierDUnlessProofFlagsSet()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "Makefile"));
        text.Should().Contain("ops-gate-full: skipping D");
        text.Should().Contain("OPS_GATE_MESH_DEEP");
        text.Should().Contain("OPS_GATE_CHAOS_LITE");
    }

    [Fact]
    public void KernelTierA_RunsCountedHostingAndPipelineSlices()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/kernel-gate-tier-a.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 40");
        text.Should().Contain("--min-tests 14");
        text.Should().Contain("KernelPhaseResolutionTests");
        text.Should().Contain("PipelineLifecycleE2ETests");
        text.Should().NotContain("dotnet test \"$INFRA\"");
    }

    [Fact]
    public void KernelTierB_RunsCountedPipelineLifecycleSlice()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/kernel-gate-tier-b.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 14");
        text.Should().Contain("PipelineLifecycleE2ETests");
        text.Should().NotContain("dotnet test src/Ashlar.Tests.Infrastructure");
    }

    [Fact]
    public void KernelTierC_RunsCountedWorkflowAndAirGappedSlices()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/kernel-gate-tier-c.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 12");
        text.Should().Contain("--min-tests 18");
        text.Should().Contain("WorkflowExecutorIntegrationTests");
        text.Should().Contain("FullyQualifiedName~AirGapped");
        text.Should().Contain("-f net10.0");
        text.Should().NotContain("dotnet test \"$INFRA\"");
    }

    [Fact]
    public void DistributionMatrixGate_RunsCountedIAshlarClientSlice()
    {
        var script = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/distribution-matrix-iashlar-client.sh"));
        var workflow = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/distribution-matrix-gate.yml"));
        script.Should().Contain("run-dotnet-test-counted.py");
        script.Should().Contain("--min-tests 1");
        script.Should().Contain("Virtual_prod_IAshlarClient_GetStatusAsync");
        script.Should().Contain("-f net10.0");
        script.Should().NotContain(
            "dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj");
        workflow.Should().Contain("scripts/distribution-matrix-iashlar-client.sh");
        workflow.Should().NotContain(
            "dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj");
    }

    [Fact]
    public void KernelTierE_RunsCountedOpenTelemetryAndPerformanceSlices()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/kernel-gate-tier-e.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 1");
        text.Should().Contain("--min-tests 3");
        text.Should().Contain("OpenTelemetryTests");
        text.Should().Contain("Ashlar.Tests.Orchestration.Performance");
        text.Should().NotContain("dotnet test \"$INFRA\"");
        text.Should().NotContain("dotnet test \"$ORCH\"");
    }

    [Fact]
    public void KernelTierE_RefusesMissingDocker()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/kernel-gate-tier-e.sh"));
        text.Should().Contain("requires a working Docker daemon");
        text.Should().Contain("refusing to skip prod-dry-run");
        text.Should().Contain("exit 2");
        text.Should().NotContain("prod-dry-run skipped");
    }

    [Fact]
    public void KernelGateWorkflow_RunsTierCOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/kernel-gate.yml"));
        text.Should().Contain("kernel-gate-tier-c");
        text.Should().NotContain("github.event_name == 'workflow_dispatch' && inputs.tier == 'c'");
    }

    [Fact]
    public void KernelGateWorkflow_RunsTierBOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/kernel-gate.yml"));
        text.Should().Contain("kernel-gate-tier-b");
        text.Should().NotContain("github.event_name == 'workflow_dispatch' && inputs.tier == 'b'");
    }

    [Fact]
    public void TestProdStyle_RunsCountedSuite()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "Makefile"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 123");
        text.Should().Contain("Category=ProdStyle&FullyQualifiedName!~ForgeEndpointsTests&FullyQualifiedName!~FrameworkVirtualProdDemosTests");
        text.Should().NotContain(
            "ASHLAR_ALLOW_MOCK=1 dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj -f net8.0 --no-build");
    }

    [Fact]
    public void ShipTierB_RunsCountedFrameworkSmoke()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/ship-gate-tier-b.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 9");
        text.Should().Contain("BaseFrameworkSmokeTests");
        text.Should().NotContain("dotnet test \"$INFRA\"");
    }

    [Fact]
    public void SecurityTierE_RunsCountedAirgappedSuiteOnNet10()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/security-gate-tier-e.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 53");
        text.Should().Contain("-f net10.0");
        text.Should().NotContain("-f net8.0");
        text.Should().NotContain("dotnet test \"$INFRA\"");
    }

    [Fact]
    public void SecurityGateWorkflow_RunsTierEHostSuiteOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/security-gate.yml"));
        text.Should().Contain("security-gate-tier-e");
        text.Should().NotContain(
            "github.event_name == 'workflow_dispatch' && (inputs.tier == 'e' || inputs.tier == 'full')");
        text.Should().NotContain("SECURITY_GATE_AIRGAPPED_CONTAINER:");
    }

    [Fact]
    public void ShipTierA_RunsCountedHostDiSmoke()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/ship-gate-tier-a.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 2");
        text.Should().Contain("AddAshlar_RegistersObservationPipeline_ByDefault");
        text.Should().NotContain("dotnet test \"$INFRA_TESTS\"");
    }

    [Fact]
    public void AutonomyObjectives_DoNotCiteRemovedApplicationsTree()
    {
        var root = Path.Combine(RepoPathResolver.FindRepoRoot(), "samples/autonomy-objectives");
        foreach (var path in Directory.GetFiles(root, "*.md"))
        {
            var text = File.ReadAllText(path);
            if (!text.Contains("pathPrefixes:", StringComparison.Ordinal))
                continue;
            text.Should().NotContain("applications/Ashlar.Samples.Dogfood", because: path);
            text.Should().Contain("samples/dogfood/", because: path);
        }
    }

    [Fact]
    public void GrpcTransportGate_RunsCountedProdStyleSuite()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/grpc-transport-gate.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 81");
        text.Should().Contain("Category=ProdStyle");
        text.Should().NotContain(
            "dotnet test src/Ashlar.Tests.Transport/Ashlar.Tests.Transport.csproj");
    }

    [Fact]
    public void GrpcTransportGateWorkflow_RunsOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/grpc-transport-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("scripts/grpc-transport-gate.sh");
        text.Should().NotContain(
            "dotnet test src/Ashlar.Tests.Transport/Ashlar.Tests.Transport.csproj");
    }

    [Fact]
    public void ProductionReadinessGate_RunsCountedPipelineAndHostDiSuites()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/production-readiness-gate-v1-tests.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 68");
        text.Should().Contain("--min-tests 2");
        text.Should().Contain("FullyQualifiedName~Pipelines");
        text.Should().NotContain(
            "dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj");
    }

    [Fact]
    public void ProductionReadinessGateWorkflow_InvokesCountedScript()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/production-readiness-gate-v1.yml"));
        text.Should().Contain("scripts/production-readiness-gate-v1-tests.sh");
        text.Should().NotContain(
            "dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj");
    }

    [Fact]
    public void McpA2AGate_RunsCountedAdapterAndProdStyleSuites()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/mcp-a2a-gate.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 40");
        text.Should().Contain("--min-tests 33");
        text.Should().Contain("--min-tests 39");
        text.Should().Contain("--min-tests 19");
        text.Should().Contain("--min-tests 7");
        text.Should().Contain("McpA2AProtocolIngress");
        text.Should().NotContain("dotnet test src/Ashlar.Mcp.Server.Tests");
    }

    [Fact]
    public void McpA2AGateWorkflow_InvokesCountedScript()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/mcp-a2a-gate.yml"));
        text.Should().Contain("scripts/mcp-a2a-gate.sh adapters");
        text.Should().Contain("scripts/mcp-a2a-gate.sh prodstyle");
        text.Should().NotContain("dotnet test src/Ashlar.Mcp.Server.Tests/Ashlar.Mcp.Server.Tests.csproj");
    }

    [Fact]
    public void OwnershipRegistry_NamesMcpA2AGate_ForProtocolTests()
    {
        foreach (var leaf in new[]
        {
            "Ashlar.Mcp.Server.Tests.csproj",
            "Ashlar.Mcp.Client.Tests.csproj",
            "Ashlar.Transport.A2A.Tests.csproj",
            "Ashlar.Transport.A2A.Server.Tests.csproj",
        })
        {
            var columns = OwnershipRow(leaf);
            columns[1].Should().Be("mcp-a2a-gate");
            columns[2].Should().Be("-");
        }
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
