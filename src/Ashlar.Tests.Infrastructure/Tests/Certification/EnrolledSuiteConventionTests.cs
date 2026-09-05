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
    public void OwnershipRegistry_NamesKernelGate_ForAiPipelineTests()
    {
        var columns = OwnershipRow("Ashlar.Tests.AI.Pipeline.csproj");
        columns[1].Should().Be("kernel-gate");
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
    public void PerfTierBaseline_FailsClosedOnRegression()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/perf-gate-baseline.sh"));
        text.Should().Contain("perf regression:");
        text.Should().Contain("perf-gate-baseline: FAIL");
        text.Should().Contain("perf-gate-baseline: PASS");
        text.Should().Contain("PERF_GATE_REPORT_DIR");
        text.Should().NotContain("PERF_GATE_STRICT_BASELINE");
        text.Should().NotContain("perf regression (advisory)");
    }

    [Fact]
    public void CertGate_MainFilterHasCollapseFloor()
    {
        var config = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/cert-gate-config.sh"));
        var guard = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/cert-gate-zero-test-guard.sh"));
        config.Should().Contain("readonly CERT_GATE_MIN_TESTS=447");
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
        text.Should().Contain("--min-tests 119");
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
        text.Should().NotContain("APPLICATION_GATE_STRICT_DOCTOR");
        text.Should().NotContain("dotnet test \"$CLI_TESTS\"");
    }

    [Fact]
    public void ApplicationTierB_FailsClosedOnDoctor()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/application-gate-tier-b.sh"));
        text.Should().Contain("doctor --json exited");
        text.Should().Contain("application-gate-tier-b: FAIL");
        text.Should().Contain("application-gate-tier-b: PASS");
        text.Should().NotContain("APPLICATION_GATE_STRICT_DOCTOR");
        text.Should().NotContain("warnings may fail strict profile");
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
        text.Should().Contain("APPLICATION_GATE_STRICT_DOCTOR");
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
    public void RcTierD_RefusesMissingGitHubCli()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/rc-gate-tier-d.sh"));
        text.Should().Contain("requires the GitHub CLI");
        text.Should().Contain("requires an authenticated GitHub CLI");
        text.Should().Contain("exit 2");
        var workflow = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/rc-gate.yml"));
        workflow.Should().Contain("GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}");
    }

    [Fact]
    public void RcTierD_RefusesAdvisorySkip()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/rc-gate-tier-d.sh"));
        text.Should().Contain("RC_GATE_GH_ADVISORY_ONLY is refused");
        text.Should().Contain("red workflows are a blocker");
        text.Should().Contain("exit 2");
        text.Should().Contain("rc-gate-tier-d: FAIL");
        text.Should().NotContain("rc-gate-tier-d: PASS (advisory)");
        var perf = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/perf-gate.sh"));
        perf.Should().NotContain("RC_GATE_GH_ADVISORY_ONLY");
    }

    [Fact]
    public void RcTierC_FailsOnMissingOrFailedBundle()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/rc-gate-tier-c.sh"));
        text.Should().Contain("release-bundle: missing");
        text.Should().Contain("rc-gate-tier-c: FAIL");
        text.Should().NotContain("RC_GATE_STRICT_EVIDENCE");
        var workflow = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/rc-gate.yml"));
        workflow.Should().Contain("ci release-bundle --profile quick");
        workflow.Should().Contain("make rc-gate-tier-c");
    }

    [Fact]
    public void RcTierC_FailsClosedOnSecurityEvidence()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/rc-gate-tier-c.sh"));
        text.Should().Contain("security: no vulnerable-packages report");
        text.Should().Contain("security: High/Critical CVEs detected");
        text.Should().Contain("rc-gate-tier-c: FAIL");
        text.Should().NotContain("RC_GATE_STRICT_SECURITY");
        var workflow = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/rc-gate.yml"));
        workflow.Should().Contain("make security-gate-tier-d");
        workflow.Should().Contain("SECURITY_GATE_STRICT_SUPPLY_CHAIN");
        workflow.Should().Contain("make rc-gate-tier-c");
    }

    [Fact]
    public void RcTierE_FailsClosedOnExceptionsPolicy()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/rc-gate-tier-e.sh"));
        text.Should().Contain("error: exceptions: missing");
        text.Should().Contain("error: exceptions: policy validation failed");
        text.Should().Contain("rc-gate-tier-e: FAIL");
        text.Should().Contain("rc-gate-tier-e: PASS");
        text.Should().NotContain("RC_GATE_STRICT_EXCEPTIONS");
        text.Should().NotContain("policy validation failed (non-strict)");
    }

    [Fact]
    public void RcGateWorkflow_RunsTierEOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/rc-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("docs/exceptions.yaml");
        text.Should().Contain("make rc-gate-tier-e");
        text.Should().Contain("github.event_name != 'workflow_dispatch' || inputs.tier == 'e'");
        text.Should().NotContain(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'e'");
    }

    [Fact]
    public void ShipTierD_RunsReleaseBundle()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/ship-gate-tier-d.sh"));
        text.Should().Contain("ci release-bundle");
        text.Should().Contain("SHIP_GATE_BUNDLE_PROFILE");
        text.Should().Contain("ship-gate-tier-d: PASS");
        text.Should().NotContain("SHIP_GATE_RUN_RUNTIME_GATE");
    }

    [Fact]
    public void DrTierC_RunsCountedHostLiteDbFallback()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/dr-gate-tier-c.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("LiteDbMeshDirectorPersistenceTests");
        text.Should().Contain("--min-tests 2");
        text.Should().Contain("dr-gate-tier-c: PASS");
        text.Should().Contain("host-litedb-backup-restore");
        text.Should().NotContain("ashlar-dr-placeholder");
        text.Should().NotContain("fake.litedb");
        text.Should().NotContain("skipped-advisory");
        text.Should().NotContain("dotnet test \"$FLEET_TESTS\"");
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
    public void OpsTierA_RunsCountedDogfoodBlocks()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/ops-gate-tier-a.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests");
        text.Should().Contain("OPS_GATE_MIN_DOGFOOD_TESTS");
        text.Should().Contain(
            "--expected-prefix \"Ashlar.Tests.Infrastructure.Tests.Dogfood.DogfoodBlock\"");
        text.Should().Contain("DogfoodBlock1Tests");
        text.Should().Contain("DogfoodBlock6Tests");
        text.Should().Contain("-f net8.0");
        text.Should().Contain("counted-dogfood-1-6");
        text.Should().NotContain("dogfood-phase-c");
        text.Should().NotContain("dotnet test \"$INFRA\"");
    }

    [Fact]
    public void OpsGateWorkflow_RunsTierAOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/ops-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("Tests/Dogfood/**");
        text.Should().Contain("scripts/ops-gate-tier-a.sh");
        text.Should().Contain("ops-gate-tier-a");
        text.Should().Contain("github.event_name != 'workflow_dispatch' || inputs.tier == 'a'");
        text.Should().NotContain(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'a'");
    }

    [Fact]
    public void OpsTierB_RunsCountedDogfoodBlocks()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/ops-gate-tier-b.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests");
        text.Should().Contain("OPS_GATE_MIN_DOGFOOD_B_TESTS");
        text.Should().Contain(
            "--expected-prefix \"Ashlar.Tests.Infrastructure.Tests.Dogfood.DogfoodBlock\"");
        text.Should().Contain("DogfoodBlock7Tests");
        text.Should().Contain("DogfoodBlock9LocalIpcTests");
        text.Should().Contain("-f net8.0");
        text.Should().Contain("counted-dogfood-7-9-ipc");
        text.Should().NotContain("dogfood-phase-de");
        text.Should().NotContain("dogfood-block9-ipc");
        text.Should().NotContain("dotnet test \"$INFRA\"");
    }

    [Fact]
    public void OpsGateWorkflow_RunsTierBOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/ops-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("scripts/ops-gate-tier-b.sh");
        text.Should().Contain("ops-gate-tier-b");
        text.Should().Contain("github.event_name != 'workflow_dispatch' || inputs.tier == 'b'");
        text.Should().NotContain(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'b'");
    }

    [Fact]
    public void OpsTierC_RunsCountedClosedLoop()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/ops-gate-tier-c.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("OPS_GATE_MIN_CLOSEDLOOP_TESTS");
        text.Should().Contain("OPS_GATE_MIN_PHASE_F_TESTS");
        text.Should().Contain("OPS_GATE_RUN_PHASE_F");
        text.Should().Contain(
            "--expected-prefix \"Ashlar.Tests.Infrastructure.Tests.Dogfood.DogfoodClosedLoopTests.\"");
        text.Should().Contain("DogfoodPhaseFTests");
        text.Should().Contain("-f net8.0");
        text.Should().Contain("counted-closed-loop");
        text.Should().NotContain("dogfood-closedloop");
        text.Should().NotContain("dogfood-phasef");
        text.Should().NotContain("dotnet test \"$INFRA\"");
    }

    [Fact]
    public void OpsGateWorkflow_RunsTierCOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/ops-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("scripts/ops-gate-tier-c.sh");
        text.Should().Contain("ops-gate-tier-c");
        text.Should().Contain("github.event_name != 'workflow_dispatch' || inputs.tier == 'c'");
        text.Should().NotContain(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'c'");
    }

    [Fact]
    public void OpsTierE_RunsOhShitDemoQuick()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/ops-gate-tier-e.sh"));
        text.Should().Contain("bash scripts/oh-shit-demo.sh --quick");
        text.Should().Contain("ops-gate-tier-e: PASS");
        text.Should().NotContain("oh-shit-demo.sh --no-build");
    }

    [Fact]
    public void OpsGateWorkflow_RunsTierEOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/ops-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("scripts/ops-gate-tier-e.sh");
        text.Should().Contain("scripts/oh-shit-demo.sh");
        text.Should().Contain("ops-gate-tier-e");
        text.Should().Contain("github.event_name != 'workflow_dispatch' || inputs.tier == 'e'");
        text.Should().NotContain(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'e'");
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
        text.Should().Contain("--min-tests 17");
        text.Should().Contain("WorkflowExecutorIntegrationTests");
        text.Should().Contain("FullyQualifiedName~AirGapped");
        text.Should().Contain("FullyQualifiedName!~EnrolledSuiteConventionTests");
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
    public void UatTier4_FailsClosedOnEmptyHelloBrick()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "tests/uat/tier4.sh"));
        text.Should().Contain("assert-dotnet-test-executed.sh");
        text.Should().Contain("HelloBrick.Tests");
    }

    [Fact]
    public void UatTier02_FailsClosedOnEmptyTeethRun()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "tests/uat/tier0-2.sh"));
        text.Should().Contain("assert-dotnet-test-executed.sh");
        text.Should().Contain("CertificationGateTeethTests");
    }

    [Fact]
    public void VerifyStandaloneBrickAuthoring_RunsCountedGeneratedSuite()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/verify-standalone-brick-authoring.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--expected-prefix \"SampleThingBrick.Tests.SampleThingBrickTests.\"");
        text.Should().Contain("--min-tests 1");
        text.Should().NotContain(
            "dotnet test \"${BRICK_OUT}/SampleThingBrick.Tests/SampleThingBrick.Tests.csproj\"");
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
    public void MeaiPipelineGate_RunsCountedSuite()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "Makefile"));
        text.Should().Contain("Ashlar.Tests.AI.Pipeline.csproj");
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--expected-prefix \"Ashlar.Tests.AI.Pipeline.\"");
        text.Should().Contain("--min-tests 43");
        text.Should().NotContain(
            "dotnet test src/Ashlar.Tests.AI.Pipeline/Ashlar.Tests.AI.Pipeline.csproj -f net8.0 -c Release --nologo");

        var workflow = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/kernel-gate.yml"));
        workflow.Should().Contain("src/Ashlar.AI.Pipeline/**");
        workflow.Should().Contain("src/Ashlar.Tests.AI.Pipeline/**");
    }

    [Fact]
    public void CiVerify_RunsCountedProdStyleAndSmoke()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "application/src/Ashlar.CLI/Commands/CiCommand.cs"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 123");
        text.Should().Contain("--min-tests 9");
        text.Should().Contain("Ashlar.Tests.Infrastructure.");
        text.Should().Contain("Category=ProdStyle&FullyQualifiedName!~ForgeEndpointsTests&FullyQualifiedName!~FrameworkVirtualProdDemosTests");
        text.Should().Contain("FullyQualifiedName~BaseFrameworkSmokeTests");
        text.Should().NotContain("$\"test \\\"{infraTestsProject}\\\"");
    }

    [Fact]
    public void Makefile_RunsMinFloorOnFullSolution()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "Makefile"));
        text.Should().Contain("run-dotnet-test-min-floor.py");
        text.Should().Contain("--min-listed 4472");
        text.Should().Contain("--project Ashlar.sln");
        text.Should().NotContain(
            "ASHLAR_ALLOW_MOCK=1 dotnet test Ashlar.sln --blame-hang-timeout 120s --blame-hang-dump-type none");
        text.Should().NotContain(
            "dotnet test Ashlar.sln --no-build --verbosity minimal --blame-hang-timeout 30s --blame-hang-dump-type none");
    }

    [Fact]
    public void Makefile_RunsCountedMeshLabSuite()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "Makefile"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 1");
        text.Should().Contain("Ashlar.Tests.Infrastructure.Tests.Mesh.");
        text.Should().Contain("Category=MeshLab");
        text.Should().NotContain(
            "ASHLAR_RUN_MESH_LAB=1 dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj -f net8.0");
    }

    [Fact]
    public void Makefile_AssertsDockerSmokeTrxFloor()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "Makefile"));
        text.Should().Contain("assert-trx-min-executed.py");
        text.Should().Contain("--min-executed 9");
        text.Should().Contain("test-results/ubuntu-8.0-base.trx");
        text.Should().Contain("test-results/alpine-8.0-base.trx");
        text.Should().Contain("test-results/debian-8.0-base.trx");
        var helper = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/assert-trx-min-executed.py"));
        helper.Should().Contain("executed_in_trx");
        helper.Should().Contain("--min-executed");
    }

    [Fact]
    public void TrustMultiEnvWorkflow_AssertsHostTrxFloor()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/test-trust-multi-env.yml"));
        text.Should().Contain("assert-trx-min-executed.py");
        text.Should().Contain("--min-executed 1");
        text.Should().Contain("trust-infra-ubuntu.trx");
        text.Should().Contain("trust-bg-ubuntu.trx");
        text.Should().Contain("trust-infra-alpine.trx");
        text.Should().Contain("trust-bg-alpine.trx");
        text.Should().Contain("trust-infra-debian.trx");
        text.Should().Contain("trust-bg-debian.trx");
        text.Should().Contain("workflow_dispatch:");
        text.Should().NotContain("pull_request:");
    }

    [Fact]
    public void CrossPlatformTestsWorkflow_AssertsHostTrxFloor()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/cross-platform-tests.yml"));
        text.Should().Contain("assert-trx-min-executed.sh");
        text.Should().Contain("--min-executed 1");
        text.Should().Contain("cross-platform-tests: no TRX written");
        text.Should().Contain("workflow_dispatch:");
        text.Should().NotContain("pull_request:");
        var helper = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/assert-trx-min-executed.sh"));
        helper.Should().Contain("assert-trx-min-executed.py");
        helper.Should().Contain("python3");
    }

    [Fact]
    public void Makefile_RunsMinFloorOnRemainingSlnfSlices()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "Makefile"));
        text.Should().Contain("run-dotnet-test-min-floor.py");
        text.Should().Contain("--min-listed 2277");
        text.Should().Contain("--min-listed 3627");
        text.Should().Contain("Ashlar.LocalDevCore.slnf");
        text.Should().Contain("Category!=ProdStyle");
        text.Should().NotContain("dotnet test Ashlar.LocalDevCore.slnf --no-build");
        text.Should().NotContain("dotnet test $(PRIME_TIME_SLNF) --no-build");
    }

    [Fact]
    public void TestPrimeTime_RunsMinFloorProdStyleSuite()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "Makefile"));
        text.Should().Contain("run-dotnet-test-min-floor.py");
        text.Should().Contain("--min-listed 365");
        text.Should().Contain("--expected-prefix \"Ashlar.Tests.\"");
        text.Should().Contain("Category=ProdStyle");
        text.Should().NotContain(
            "dotnet test $(PRIME_TIME_SLNF) --no-build \\\n\t  --filter \"Category=ProdStyle\"");
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
        text.Should().Contain("doctor --json exited");
        text.Should().NotContain("SHIP_GATE_STRICT_DOCTOR");
        text.Should().NotContain("dotnet test \"$INFRA\"");
    }

    [Fact]
    public void ValidateSafe_RunsCountedFrameworkSmoke()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/validate-safe.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 9");
        text.Should().Contain("BaseFrameworkSmokeTests");
        text.Should().NotContain(
            "dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj");
    }

    [Fact]
    public void ReadinessGateLocal_RunsCountedCliSuite()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/readiness-gate-local.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 200");
        text.Should().Contain("FullyQualifiedName!~UnitTestBridgeTests");
        text.Should().Contain("application-tests-cli-full");
        text.Should().NotContain(
            "dotnet test application/src/Ashlar.Tests.CLI/Ashlar.Tests.CLI.csproj");
    }

    [Fact]
    public void ShipTierB_FailsClosedOnDoctor()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/ship-gate-tier-b.sh"));
        text.Should().Contain("doctor --json exited");
        text.Should().Contain("ship-gate-tier-b: FAIL");
        text.Should().Contain("ship-gate-tier-b: PASS");
        text.Should().NotContain("SHIP_GATE_STRICT_DOCTOR");
        text.Should().NotContain("warnings may fail strict profile");
    }

    [Fact]
    public void ShipGateWorkflow_RunsTierBOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/ship-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("ship-gate-tier-b");
        text.Should().Contain("SHIP_GATE_STRICT_DOCTOR");
        text.Should().NotContain(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'b'");
    }

    [Fact]
    public void ShipGateWorkflow_RunsTierDOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/ship-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("scripts/ship-gate-tier-d.sh");
        text.Should().Contain("ship-gate-tier-d");
        text.Should().Contain("github.event_name != 'workflow_dispatch'");
        text.Should().NotContain(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'd'");
    }

    [Fact]
    public void ShipGateWorkflow_RunsTierCOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/ship-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("scripts/ship-gate-tier-c.sh");
        text.Should().Contain("ship-gate-tier-c");
        text.Should().Contain("github.event_name != 'workflow_dispatch' || inputs.tier == 'c'");
        text.Should().NotContain(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'c'");
    }

    [Fact]
    public void SecurityTierD_FailsWhenScanCannotRun()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/security-gate-tier-d.sh"));
        text.Should().Contain("security-gate-tier-d: FAIL");
        text.Should().Contain("supply-chain scan could not run");
        text.Should().Contain("security-gate-tier-d: PASS");
        text.Should().NotContain("Some scans failed — see reports");
        var workflow = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/security-gate.yml"));
        workflow.Should().Contain("github.event_name != 'workflow_dispatch' || inputs.tier == 'd'");
        workflow.Should().Contain("SECURITY_GATE_STRICT_SUPPLY_CHAIN: \"1\"");
    }

    [Fact]
    public void SecurityTierD_FailsClosedOnVulnerablePackages()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/security-gate-tier-d.sh"));
        text.Should().Contain("Vulnerable packages detected");
        text.Should().Contain("security-gate-tier-d: FAIL");
        text.Should().Contain("security-gate-tier-d: PASS");
        text.Should().NotContain("SECURITY_GATE_STRICT_SUPPLY_CHAIN");
        text.Should().NotContain("set SECURITY_GATE_STRICT_SUPPLY_CHAIN=1 to fail");
    }

    [Fact]
    public void SecurityTierE_RunsCountedAirgappedSuiteOnNet10()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/security-gate-tier-e.sh"));
        text.Should().Contain("run-dotnet-test-counted.py");
        text.Should().Contain("--min-tests 52");
        text.Should().Contain("FullyQualifiedName!~EnrolledSuiteConventionTests");
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
        text.Should().Contain("pull_request:");
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

    [Fact]
    public void IngressUnitGateWorkflow_RunsOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/ingress-unit-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("make ingress-unit-gate");
        text.Should().Contain("scripts/ingress-unit-gate.sh");
    }

    [Fact]
    public void OnboardingDocsGuardWorkflow_RunsOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/onboarding-docs-guard.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("docs/ProjectTiers.md");
        text.Should().Contain("Referenced repo paths must exist");
    }

    [Fact]
    public void E2eLoop_HasCollapseFloor()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/e2e-loop.sh"));
        text.Should().Contain("E2E_LOOP_MIN_SCENARIOS");
        text.Should().Contain("E2E_LOOP_MIN_SCENARIOS=143");
        text.Should().Contain("E2E_LOOP_MIN_SCENARIOS=137");
        text.Should().Contain("discovery collapsed");
    }

    [Fact]
    public void PackHostingGraphAlignmentWorkflow_RunsOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/pack-hosting-graph-alignment.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("verify-pack-ashlar-hosting-graph-alignment.py");
        text.Should().Contain("src/**/*.csproj");
    }

    [Fact]
    public void OnboardingQuickstartWorkflow_RunsNativeLaneOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/onboarding-quickstart-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("scripts/setup/setup.sh check");
        text.Should().Contain("github.event_name != 'pull_request'");
    }

    [Fact]
    public void EnvironmentSetupGateWorkflow_RunsNativeMatrixOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/environment-setup-gate-v1.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("scripts/setup/setup.sh check");
        text.Should().Contain("./scripts/setup/setup.ps1 -Mode check");
        text.Should().Contain("github.event_name != 'pull_request'");
    }

    [Fact]
    public void OptimizeAgentClusterGateWorkflow_RunsOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/optimize-agent-cluster-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("optimize_agent_cluster.sh");
        text.Should().Contain("Unified workflow");
        text.Should().Contain("--skip-optimize");
    }

    [Fact]
    public void RuntimeReleaseGateWorkflow_RunsCoreAndVisualLanesOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/runtime-release-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("runtime release-gate");
        text.Should().Contain("--mode ${{ matrix.lane }}");
        text.Should().Contain("--allow-mock");
    }

    [Fact]
    public void InstallerBruteforceGateWorkflow_RunsOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/installer-bruteforce-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("scripts/install/bruteforce-matrix.sh");
        text.Should().Contain("scripts/setup/**");
        text.Should().Contain("scripts/install/**");
    }

    [Fact]
    public void MultiPlatformTestCommand_FailsClosedOnZeroTests()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "application/src/Ashlar.CLI/Commands/MultiPlatformTestCommand.cs"));
        text.Should().Contain("DotnetTestTool.HasExecutedTests");
        text.Should().Contain("DotnetTestTool.Succeeded");
        text.Should().Contain("Passed:\\s*(\\d+)");
        text.Should().NotContain("Passed = process.ExitCode == 0 && failed == 0");
        text.Should().NotContain("Passed = runResult.Success && failed == 0");
    }

    [Fact]
    public void MultiPlatformTestBase_FailsClosedOnZeroTests()
    {
        var baseline = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "src/Ashlar.Tests.Infrastructure/Tests/MultiPlatform/MultiPlatformTestBase.cs"));
        baseline.Should().Contain("RunPassed");
        baseline.Should().Contain("DotnetTestTool.HasExecutedTests");
        baseline.Should().Contain("Passed:\\s*(\\d+)");
        baseline.Should().NotContain("total == 0 || failed == 0");
        baseline.Should().NotContain("output.Contains(\"passed\"");

        var ios = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "src/Ashlar.Tests.Infrastructure/Tests/MultiPlatform/IosTest.cs"));
        ios.Should().Contain("RunPassed");
        ios.Should().NotContain("total == 0 || failed == 0");
    }

    [Fact]
    public void TestPortableCommand_FailsClosedOnZeroTests()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "application/src/Ashlar.CLI/Commands/TestPortableCommand.cs"));
        text.Should().Contain("DotnetTestTool.HasExecutedTests");
        text.Should().Contain("No tests matched the filter");
        text.Should().Contain("ExitCode.ValidationFailed");
        text.Should().NotContain("passed = process.ExitCode == 0");
    }

    [Fact]
    public void TestCommand_FailsClosedOnZeroTests()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "application/src/Ashlar.CLI/Commands/TestCommandRunner.cs"));
        text.Should().Contain("TotalTests < 1");
        text.Should().Contain("No tests matched the filter");
        text.Should().Contain("ExitCode.ValidationFailed");
    }

    [Fact]
    public void SelfExtendCommand_FailsClosedOnZeroTests()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "application/src/Ashlar.CLI/Commands/SelfExtendCommand.cs"));
        text.Should().Contain("No tests discovered for filter");
        text.Should().Contain("run.ExitCode == 0 && totalTests <= 0");
        text.Should().NotContain("allow-mock: skipping strict discoverability");
        text.Should().NotContain("if (allowMock)\n                return run;");
    }

    [Fact]
    public void TestRunRunnerAdapter_FailsClosedOnZeroTests()
    {
        var adapter = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "src/Ashlar.BackgroundAgents.HostRunners/TestRunRunnerAdapter.cs"));
        adapter.Should().Contain("result.FailedTests == 0 && result.TotalTests >= 1");
        adapter.Should().Contain("No tests matched the filter");

        var registry = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "src/Ashlar.BackgroundAgents/Registry/BackgroundAgentRegistry.cs"));
        registry.Should().Contain("result.FailedTests > 0 || result.TotalTests < 1");
    }

    [Fact]
    public void DotNetRegressionTestRunner_FailsClosedOnZeroTests()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "src/Ashlar.Infrastructure/Analysis/BrickAnalyzer/DotNetRegressionTestRunner.cs"));
        text.Should().Contain("failed == 0 && passed >= 1");
        text.Should().Contain("No tests matched the filter");
    }

    [Fact]
    public void ValidationServiceAdapter_FailsClosedOnZeroTests()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "src/Ashlar.Infrastructure/Validation/Adapters/ValidationServiceAdapter.cs"));
        text.Should().Contain("totalTestsFailed == 0 && totalTestsRun >= 1");
        text.Should().Contain("Passed = false");
        text.Should().Contain("No test projects found");
        text.Should().NotContain("validation skipped");
        text.Should().NotContain("even if no tests were run");
    }

    [Fact]
    public void DotnetTestTool_FailsClosedOnZeroTests()
    {
        var tool = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "src/Ashlar.Tools.Dev/DotnetTestTool.cs"));
        tool.Should().Contain("HasExecutedTests");
        tool.Should().Contain("No test is available");
        tool.Should().Contain("No test matches");
        tool.Should().Contain("Passed:\\s*(\\d+)");
        tool.Should().NotContain("ok = code == 0");

        var forge = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "src/Ashlar.Tools.Dev/ForgeTestTool.cs"));
        forge.Should().Contain("DotnetTestTool.Succeeded");
        forge.Should().NotContain("ok = code == 0");

        var proposals = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "application/src/Ashlar.CLI/Commands/BackgroundAgent/ProposalsBackgroundAgentCommand.cs"));
        proposals.Should().Contain("DotnetTestTool.Succeeded");
        proposals.Should().NotContain("tCode == 0 && !tTimedOut");
    }

    [Fact]
    public void DotNetInstanceSpawner_FailsClosedOnZeroTests()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "src/Ashlar.Infrastructure/ParallelTesting/DotNetInstanceSpawner.cs"));
        text.Should().Contain("HasExecutedTests");
        text.Should().Contain("No test is available");
        text.Should().Contain("No test matches");
        text.Should().Contain("No tests matched the filter");
        text.Should().NotContain("return (proc.ExitCode == 0, output)");
    }

    [Fact]
    public void ParallelTestAggregators_FailClosedOnEmptyInstances()
    {
        var collector = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "src/Ashlar.Infrastructure/ParallelTesting/ResultCollector.cs"));
        collector.Should().Contain("instances.Count > 0 && instances.All");

        var aggregator = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "src/Ashlar.Infrastructure/Adaptation/InstanceResultAggregator.cs"));
        aggregator.Should().Contain("results.Count > 0 && results.All");
    }

    [Fact]
    public void TestRunnerAdapter_LooksForCliTestsUnderApplicationSrc()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "src/Ashlar.Infrastructure/Testing/TestRunnerAdapter.cs"));
        text.Should().Contain("\"Ashlar.Tests.CLI\"");
        text.Should().Contain("\"application\", \"src\"");
    }

    [Fact]
    public void WorkflowRegressionGate_FailsClosedOnEmptyTestLocal()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/workflow-regression-gate.sh"));
        text.Should().Contain("assert-test-local-floor.py");
        text.Should().Contain("WorkflowCommandTests");
        text.Should().Contain("workflow-regression-gate: FAIL");
        text.Should().Contain("workflow-regression-gate: PASS");
        text.Should().Contain("workflow baseline promote");
    }

    [Fact]
    public void WorkflowRegressionGateWorkflow_RunsOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/workflow-regression-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("scripts/workflow-regression-gate.sh");
        text.Should().Contain("scripts/lib/assert-test-local-floor.py");
        text.Should().Contain("application/src/Ashlar.CLI/**");
    }

    [Fact]
    public void DogfoodTestCommand_FailsClosedOnZeroTests()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "application/src/Ashlar.CLI/Commands/DogfoodTestCommand.cs"));
        text.Should().Contain("DotnetTestTool.HasExecutedTests");
        text.Should().Contain("No tests matched the filter");
        text.Should().Contain("ExitCode.ValidationFailed");
        text.Should().NotContain("var passed = testResult == 0;");
    }

    [Fact]
    public void Makefile_RunsCountedDogfoodBlocks()
    {
        var makefile = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "Makefile"));
        makefile.Should().Contain("scripts/run-dogfood-block.sh");
        makefile.Should().Contain("DogfoodBlock1Tests");
        makefile.Should().Contain("DogfoodPhaseFTests");
        makefile.Should().NotContain(
            "dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj --filter \"FullyQualifiedName~DogfoodBlock1Tests\"");

        var script = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "scripts/run-dogfood-block.sh"));
        script.Should().Contain("run-dotnet-test-counted.py");
        script.Should().Contain("--expected-prefix \"Ashlar.Tests.Infrastructure.Tests.Dogfood.${CLASS}.\"");
        script.Should().NotContain("dotnet test \"$INFRA\"");
    }

    [Fact]
    public void TestMultiEnvCommand_FailsClosedOnZeroTests()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            "application/src/Ashlar.CLI/Commands/TestMultiEnvCommand.cs"));
        text.Should().Contain("DotnetTestTool.HasExecutedTests");
        text.Should().Contain("EnvRunPassed");
        text.Should().NotContain("passed == 0 && total == 0 && runExit != 0");
        text.Should().NotContain("if (runExit != 0) failed++;");
    }

    [Fact]
    public void RcGateWorkflow_RunsTierCOnPullRequest()
    {
        var text = File.ReadAllText(Path.Combine(RepoPathResolver.FindRepoRoot(),
            ".github/workflows/rc-gate.yml"));
        text.Should().Contain("pull_request:");
        text.Should().Contain("scripts/rc-gate*.sh");
        text.Should().Contain("ci release-bundle --profile quick");
        text.Should().Contain("make rc-gate-tier-c");
        text.Should().Contain("github.event_name != 'workflow_dispatch'");
        text.Should().NotContain(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'c'");
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
