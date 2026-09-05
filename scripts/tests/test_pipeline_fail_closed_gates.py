#!/usr/bin/env python3
"""Guard Production Readiness Gate v1 fail-closed helpers and local mirrors."""
from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HELPER = ROOT / "scripts" / "lib" / "assert-pipeline-fail-closed.py"
MIRROR_SCRIPTS = (
    ROOT / "scripts" / "kernel-gate-tier-b.sh",
    ROOT / "scripts" / "ship-gate-tier-a.sh",
    ROOT / "scripts" / "dr-gate-tier-a.sh",
)
WORKFLOW = ROOT / ".github" / "workflows" / "production-readiness-gate-v1.yml"


def _payload(*, ok: bool, state: str, ingest_error: str, extra: dict | None = None) -> dict:
    body = {
        "ok": ok,
        "data": {
            "state": state,
            "stages": [{"stageId": "ingest", "error": ingest_error}],
        },
    }
    if extra:
        body["data"].update(extra)
    return body


class FailClosedHelperTests(unittest.TestCase):
    def _write(self, directory: Path, name: str, payload: dict) -> Path:
        path = directory / name
        path.write_text(json.dumps(payload) + "\n", encoding="utf-8")
        return path

    def _run(self, *args: str) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [sys.executable, str(HELPER), *args],
            check=False,
            capture_output=True,
            text=True,
        )

    def test_fail_closed_accepts_unconfigured_placeholder(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            log = self._write(
                Path(tmp),
                "unconfigured.log",
                _payload(
                    ok=False,
                    state="Failed",
                    ingest_error="No deterministic pipeline adapter is configured",
                ),
            )
            result = self._run("fail-closed", str(log), "unconfigured")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("fail-closed PASS", result.stdout)

    def test_fail_closed_rejects_fabricated_success(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            log = self._write(
                Path(tmp),
                "success.log",
                _payload(ok=True, state="Completed", ingest_error=""),
            )
            result = self._run("fail-closed", str(log), "unconfigured")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("must not report ok=true", result.stderr + result.stdout)

    def test_resume_stays_failed_and_finds_source(self) -> None:
        failed = _payload(
            ok=False,
            state="Failed",
            ingest_error="No deterministic pipeline adapter is configured",
        )
        with tempfile.TemporaryDirectory() as tmp:
            source = self._write(Path(tmp), "source.log", failed)
            target = self._write(Path(tmp), "target.log", failed)
            result = self._run("resume", str(source), str(target))
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_resume_rejects_missing_prior_run(self) -> None:
        source = _payload(ok=False, state="Failed", ingest_error="hook")
        target = _payload(
            ok=False,
            state="Failed",
            ingest_error="no prior run was found",
            extra={"message": "no prior run was found"},
        )
        with tempfile.TemporaryDirectory() as tmp:
            source_log = self._write(Path(tmp), "source.log", source)
            target_log = self._write(Path(tmp), "target.log", target)
            result = self._run("resume", str(source_log), str(target_log))
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("did not find the persisted source run", result.stderr + result.stdout)

    def test_resume_rejects_fabricated_completion(self) -> None:
        source = _payload(ok=False, state="Failed", ingest_error="hook")
        target = _payload(ok=True, state="Completed", ingest_error="")
        with tempfile.TemporaryDirectory() as tmp:
            source_log = self._write(Path(tmp), "source.log", source)
            target_log = self._write(Path(tmp), "target.log", target)
            result = self._run("resume", str(source_log), str(target_log))
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("must not report ok=true", result.stderr + result.stdout)


class GrpcTransportGateCountedTests(unittest.TestCase):
    def test_script_runs_counted_prodstyle_suite(self) -> None:
        text = (ROOT / "scripts" / "grpc-transport-gate.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 81", text)
        self.assertIn("Category=ProdStyle", text)
        self.assertIn("Ashlar.Tests.Transport.", text)
        self.assertNotIn(
            "dotnet test src/Ashlar.Tests.Transport/Ashlar.Tests.Transport.csproj",
            text,
        )

    def test_workflow_invokes_counted_script_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "grpc-transport-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("pull_request:", text)
        self.assertIn("scripts/grpc-transport-gate.sh", text)
        self.assertNotIn(
            "dotnet test src/Ashlar.Tests.Transport/Ashlar.Tests.Transport.csproj",
            text,
        )

    def test_kernel_tier_c_invokes_counted_grpc_script(self) -> None:
        text = (ROOT / "scripts" / "kernel-gate-tier-c.sh").read_text(encoding="utf-8")
        self.assertIn("scripts/grpc-transport-gate.sh", text)
        self.assertNotIn('dotnet test "$TRANSPORT"', text)


class ProductionReadinessGateCountedTests(unittest.TestCase):
    def test_script_runs_counted_pipeline_and_host_di_suites(self) -> None:
        text = (ROOT / "scripts" / "production-readiness-gate-v1-tests.sh").read_text(
            encoding="utf-8"
        )
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 68", text)
        self.assertIn("--min-tests 2", text)
        self.assertIn('FullyQualifiedName~Pipelines', text)
        self.assertIn("AddAshlar_RegistersObservationPipeline_ByDefault", text)
        self.assertNotIn(
            "dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj",
            text,
        )

    def test_workflow_invokes_counted_script(self) -> None:
        text = WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("pull_request:", text)
        self.assertIn("scripts/production-readiness-gate-v1-tests.sh", text)
        self.assertNotIn(
            "dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj",
            text,
        )


class MirrorScriptContractTests(unittest.TestCase):
    def test_local_gates_call_shared_helper(self) -> None:
        for script in MIRROR_SCRIPTS:
            text = script.read_text(encoding="utf-8")
            self.assertIn(
                "assert-pipeline-fail-closed.py",
                text,
                f"{script.name} must use the shared fail-closed helper",
            )
            self.assertNotIn(
                'state") != "Completed"',
                text,
                f"{script.name} still expects fabricated Completed",
            )
            self.assertNotIn(
                "did not complete successfully",
                text,
                f"{script.name} still expects fabricated success",
            )

    def test_canonical_workflow_uses_shared_helper(self) -> None:
        text = WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("assert-pipeline-fail-closed.py", text)
        self.assertIn("scripts/lib/assert-pipeline-fail-closed.py", text)


class ShipGateTierCCanonicalVersionTests(unittest.TestCase):
    def test_ship_gate_tier_c_defaults_to_canonical_version_file(self) -> None:
        text = (ROOT / "scripts" / "ship-gate-tier-c.sh").read_text(encoding="utf-8")
        self.assertNotIn(
            "${SHIP_GATE_VERSION:-0.0.0-ship-gate-local}",
            text,
            "dummy prerelease must not be the default SHIP_GATE_VERSION",
        )
        self.assertIn("tr -d '[:space:]' < VERSION", text)
        self.assertIn("SHIP_GATE_VERSION", text)

    def test_preflight_rejects_dummy_ship_gate_prerelease(self) -> None:
        run = subprocess.run(
            ["bash", str(ROOT / "scripts" / "release-preflight-local.sh"), "0.0.0-ship-gate-local"],
            cwd=ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
        )
        self.assertNotEqual(0, run.returncode)
        self.assertIn("not valid semver", run.stdout)


class CompositionMeshTierABCountedTests(unittest.TestCase):
    def test_tier_a_runs_counted_pipeline_suite(self) -> None:
        text = (ROOT / "scripts" / "composition-mesh-gate-tier-a.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 64", text)
        self.assertIn("Ashlar.Tests.Infrastructure.Tests.Pipelines", text)
        self.assertNotIn('dotnet test "$INFRA"', text)

    def test_tier_b_runs_counted_cli_bridge_rows(self) -> None:
        text = (ROOT / "scripts" / "composition-mesh-gate-tier-b.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 3", text)
        self.assertIn("DisplayName~PipelineCommand", text)
        self.assertNotIn('dotnet test "$CLI"', text)


class CompositionMeshTierCFleetHostTests(unittest.TestCase):
    def test_tier_c_runs_counted_fleet_suite_on_net8(self) -> None:
        text = (ROOT / "scripts" / "composition-mesh-gate-tier-c.sh").read_text(encoding="utf-8")
        self.assertIn("Ashlar.Commercial.Tests.Fleet.csproj", text)
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--expected-prefix \"Ashlar.Commercial.Tests.Fleet.\"", text)
        self.assertIn("--min-tests 176", text)
        self.assertIn("-f net8.0", text)
        self.assertNotIn('dotnet test "$FLEET_TESTS"', text)

    def test_tier_c_runs_counted_fleet_host_suite_on_net10(self) -> None:
        text = (ROOT / "scripts" / "composition-mesh-gate-tier-c.sh").read_text(encoding="utf-8")
        self.assertIn("Ashlar.Commercial.Tests.Fleet.Host", text)
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("-f net10.0", text)
        self.assertIn("--min-tests 4", text)

    def test_tier_c_runs_counted_mesh_director_suite_on_net8(self) -> None:
        text = (ROOT / "scripts" / "composition-mesh-gate-tier-c.sh").read_text(encoding="utf-8")
        self.assertIn("Ashlar.Commercial.Tests.MeshDirector", text)
        self.assertIn("-f net8.0", text)
        self.assertIn("--expected-prefix \"Ashlar.Commercial.Tests.MeshDirector.\"", text)


class CompatGateCountedTests(unittest.TestCase):
    def test_compat_tier_a_runs_fleet_checkpoint_on_commercial_suite(self) -> None:
        text = (ROOT / "scripts" / "compat-gate-tier-a.sh").read_text(encoding="utf-8")
        self.assertIn("Ashlar.Commercial.Tests.Fleet.csproj", text)
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 1", text)
        self.assertIn("--min-tests 4", text)
        self.assertIn("MeshTaskExecutionServiceTests.MigrateForCheckpointAsync", text)
        self.assertIn("CompositionRegistryValidationTests", text)
        self.assertNotIn('dotnet test "$INFRA"', text)
        self.assertNotIn('dotnet test "$FLEET_TESTS"', text)

    def test_compat_tier_c_runs_counted_configuration_and_kernel_phase(self) -> None:
        text = (ROOT / "scripts" / "compat-gate-tier-c.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 2", text)
        self.assertIn("--min-tests 4", text)
        self.assertIn("AddPipelineCompositionLayer_WithConfiguration", text)
        self.assertIn("KernelPhaseResolutionTests", text)
        self.assertNotIn('dotnet test "$INFRA"', text)

    def test_compat_gate_workflow_runs_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "compat-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("pull_request:", text)
        self.assertIn("Ashlar.Commercial.Tests.Fleet", text)
        self.assertIn("scripts/compat-gate.sh", text)


class DrGateCountedTests(unittest.TestCase):
    def test_dr_tier_b_runs_counted_knowledge_store_slice(self) -> None:
        text = (ROOT / "scripts" / "dr-gate-tier-b.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 8", text)
        self.assertIn("LiteDbUserKnowledgeLogStoreTests", text)
        self.assertNotIn('dotnet test "$INFRA"', text)

    def test_dr_gate_workflow_runs_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "dr-gate.yml").read_text(encoding="utf-8")
        self.assertIn("pull_request:", text)
        self.assertIn("LiteDbUserKnowledgeLogStoreTests", text)

    def test_dr_tier_c_runs_counted_host_litedb_fallback(self) -> None:
        text = (ROOT / "scripts" / "dr-gate-tier-c.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("LiteDbMeshDirectorPersistenceTests", text)
        self.assertIn("--min-tests 2", text)
        self.assertIn("host-litedb-backup-restore", text)
        self.assertIn("dr-gate-tier-c: PASS", text)
        self.assertNotIn("ashlar-dr-placeholder", text)
        self.assertNotIn("fake.litedb", text)
        self.assertNotIn("skipped-advisory", text)
        self.assertNotIn("PASS (advisory)", text)
        self.assertNotIn('dotnet test "$FLEET_TESTS"', text)


class PerfGateCountedTests(unittest.TestCase):
    def test_perf_tier_a_runs_counted_orch_and_background_slices(self) -> None:
        text = (ROOT / "scripts" / "perf-gate-tier-a.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 3", text)
        self.assertIn("--min-tests 9", text)
        self.assertIn("Ashlar.Tests.Orchestration.Performance", text)
        self.assertIn("Ashlar.Tests.BackgroundAgents.Performance", text)
        self.assertNotIn('dotnet test "$ORCH"', text)
        self.assertNotIn('dotnet test "$BG"', text)

    def test_perf_gate_workflow_runs_tier_a_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "perf-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("pull_request:", text)
        self.assertIn("perf-gate-tier-a.sh", text)
        self.assertIn("github.event_name != 'pull_request'", text)


class CertGateCollapseFloorTests(unittest.TestCase):
    def test_cert_gate_config_pins_collapse_floor(self) -> None:
        config = (ROOT / "scripts" / "cert-gate-config.sh").read_text(encoding="utf-8")
        guard = (ROOT / "scripts" / "cert-gate-zero-test-guard.sh").read_text(encoding="utf-8")
        self.assertIn("readonly CERT_GATE_MIN_TESTS=447", config)
        self.assertIn("CERT_GATE_MIN_TESTS", guard)
        self.assertIn("discovery collapsed", guard)

    def test_cert_gate_main_filter_excludes_enrolled_suite_conventions(self) -> None:
        config = (ROOT / "scripts" / "cert-gate-config.sh").read_text(encoding="utf-8")
        self.assertIn("FullyQualifiedName!~EnrolledSuiteConventionTests", config)

    def test_cert_gate_runs_counted_enrolled_suite_conventions(self) -> None:
        text = (ROOT / "scripts" / "run-cert-gate.sh").read_text(encoding="utf-8")
        self.assertIn("EnrolledSuiteConventionTests", text)
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 93", text)


class CertGateAnalyzerCountedTests(unittest.TestCase):
    def test_cert_gate_runs_counted_analyzer_suite(self) -> None:
        text = (ROOT / "scripts" / "run-cert-gate.sh").read_text(encoding="utf-8")
        self.assertIn("Ashlar.Analyzers.Tests", text)
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 56", text)
        self.assertIn("-f net8.0", text)

    def test_cert_gate_runs_counted_contracts_suite(self) -> None:
        text = (ROOT / "scripts" / "run-cert-gate.sh").read_text(encoding="utf-8")
        self.assertIn("Ashlar.Tests.Contracts", text)
        self.assertIn("--min-tests 18", text)
        self.assertIn('--expected-prefix "Ashlar.Tests.Contracts."', text)


class McpA2AGateCountedTests(unittest.TestCase):
    def test_script_runs_counted_adapter_and_prodstyle_suites(self) -> None:
        text = (ROOT / "scripts" / "mcp-a2a-gate.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 40", text)
        self.assertIn("--min-tests 33", text)
        self.assertIn("--min-tests 39", text)
        self.assertIn("--min-tests 19", text)
        self.assertIn("--min-tests 7", text)
        self.assertIn("Ashlar.Mcp.Server.Tests.", text)
        self.assertIn("Ashlar.Mcp.Client.Tests.", text)
        self.assertIn("Ashlar.Transport.A2A.Tests.", text)
        self.assertIn("Ashlar.Transport.A2A.Server.Tests.", text)
        self.assertIn("McpA2AProtocolIngress", text)
        self.assertNotIn("dotnet test src/Ashlar.Mcp.Server.Tests", text)

    def test_workflow_invokes_counted_script(self) -> None:
        text = (ROOT / ".github" / "workflows" / "mcp-a2a-gate.yml").read_text(encoding="utf-8")
        self.assertIn("scripts/mcp-a2a-gate.sh adapters", text)
        self.assertIn("scripts/mcp-a2a-gate.sh prodstyle", text)
        self.assertNotIn(
            "dotnet test src/Ashlar.Mcp.Server.Tests/Ashlar.Mcp.Server.Tests.csproj",
            text,
        )
        self.assertNotIn(
            "dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj",
            text,
        )


class IngressUnitGateCountedTests(unittest.TestCase):
    def test_ingress_unit_gate_runs_counted_sns_and_dynamodb_suites(self) -> None:
        text = (ROOT / "scripts" / "ingress-unit-gate.sh").read_text(encoding="utf-8")
        self.assertIn("Ashlar.Ingress.AwsSns.Tests", text)
        self.assertIn("Ashlar.Ingress.DynamoDb.Tests", text)
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 11", text)
        self.assertIn("--min-tests 2", text)
        self.assertIn("-f net8.0", text)

    def test_ashlar_ready_invokes_ingress_unit_gate(self) -> None:
        text = (ROOT / "scripts" / "ashlar-ready-gate.sh").read_text(encoding="utf-8")
        self.assertIn("make ingress-unit-gate", text)

    def test_ingress_unit_gate_workflow_runs_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "ingress-unit-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("pull_request:", text)
        self.assertIn("make ingress-unit-gate", text)
        self.assertIn("scripts/ingress-unit-gate.sh", text)


class OnboardingDocsGuardWorkflowTests(unittest.TestCase):
    def test_onboarding_docs_guard_workflow_runs_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "onboarding-docs-guard.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("pull_request:", text)
        self.assertIn("docs/ProjectTiers.md", text)
        self.assertIn("Referenced repo paths must exist", text)


class E2eLoopCollapseFloorTests(unittest.TestCase):
    def test_e2e_loop_has_os_aware_collapse_floor(self) -> None:
        text = (ROOT / "scripts" / "e2e-loop.sh").read_text(encoding="utf-8")
        self.assertIn("E2E_LOOP_MIN_SCENARIOS", text)
        self.assertIn("E2E_LOOP_MIN_SCENARIOS=143", text)
        self.assertIn("E2E_LOOP_MIN_SCENARIOS=137", text)
        self.assertIn("discovery collapsed", text)


class PackHostingGraphAlignmentWorkflowTests(unittest.TestCase):
    def test_pack_hosting_graph_alignment_workflow_runs_on_pull_request(self) -> None:
        text = (
            ROOT / ".github" / "workflows" / "pack-hosting-graph-alignment.yml"
        ).read_text(encoding="utf-8")
        self.assertIn("pull_request:", text)
        self.assertIn("verify-pack-ashlar-hosting-graph-alignment.py", text)
        self.assertIn("src/**/*.csproj", text)


class OnboardingQuickstartWorkflowTests(unittest.TestCase):
    def test_onboarding_quickstart_workflow_runs_native_lane_on_pull_request(self) -> None:
        text = (
            ROOT / ".github" / "workflows" / "onboarding-quickstart-gate.yml"
        ).read_text(encoding="utf-8")
        self.assertIn("pull_request:", text)
        self.assertIn("scripts/setup/setup.sh check", text)
        self.assertIn("github.event_name != 'pull_request'", text)


class EnvironmentSetupGateWorkflowTests(unittest.TestCase):
    def test_environment_setup_gate_workflow_runs_native_matrix_on_pull_request(self) -> None:
        text = (
            ROOT / ".github" / "workflows" / "environment-setup-gate-v1.yml"
        ).read_text(encoding="utf-8")
        self.assertIn("pull_request:", text)
        self.assertIn("scripts/setup/setup.sh check", text)
        self.assertIn("./scripts/setup/setup.ps1 -Mode check", text)
        self.assertIn("github.event_name != 'pull_request'", text)


class OptimizeAgentClusterGateWorkflowTests(unittest.TestCase):
    def test_optimize_agent_cluster_gate_workflow_runs_on_pull_request(self) -> None:
        text = (
            ROOT / ".github" / "workflows" / "optimize-agent-cluster-gate.yml"
        ).read_text(encoding="utf-8")
        self.assertIn("pull_request:", text)
        self.assertIn("optimize_agent_cluster.sh", text)
        self.assertIn("Unified workflow", text)
        self.assertIn("--skip-optimize", text)


class RuntimeReleaseGateWorkflowTests(unittest.TestCase):
    def test_runtime_release_gate_workflow_runs_core_and_visual_on_pull_request(self) -> None:
        text = (
            ROOT / ".github" / "workflows" / "runtime-release-gate.yml"
        ).read_text(encoding="utf-8")
        self.assertIn("pull_request:", text)
        self.assertIn("runtime release-gate", text)
        self.assertIn("--mode ${{ matrix.lane }}", text)
        self.assertIn("--allow-mock", text)


class InstallerBruteforceGateWorkflowTests(unittest.TestCase):
    def test_installer_bruteforce_gate_workflow_runs_on_pull_request(self) -> None:
        text = (
            ROOT / ".github" / "workflows" / "installer-bruteforce-gate.yml"
        ).read_text(encoding="utf-8")
        self.assertIn("pull_request:", text)
        self.assertIn("scripts/install/bruteforce-matrix.sh", text)
        self.assertIn("scripts/setup/**", text)
        self.assertIn("scripts/install/**", text)


class TestCommandFailClosedTests(unittest.TestCase):
    def test_test_command_fails_closed_on_zero_tests(self) -> None:
        text = (
            ROOT / "application" / "src" / "Ashlar.CLI" / "Commands" / "TestCommandRunner.cs"
        ).read_text(encoding="utf-8")
        self.assertIn("TotalTests < 1", text)
        self.assertIn("No tests matched the filter", text)
        self.assertIn("ExitCode.ValidationFailed", text)


class WorkflowRegressionGateFailClosedTests(unittest.TestCase):
    def test_test_runner_adapter_looks_for_cli_tests_under_application_src(self) -> None:
        text = (
            ROOT / "src" / "Ashlar.Infrastructure" / "Testing" / "TestRunnerAdapter.cs"
        ).read_text(encoding="utf-8")
        self.assertIn('"Ashlar.Tests.CLI"', text)
        self.assertIn('"application", "src"', text)

    def test_workflow_regression_gate_fails_closed_on_empty_test_local(self) -> None:
        text = (ROOT / "scripts" / "workflow-regression-gate.sh").read_text(
            encoding="utf-8"
        )
        self.assertIn("assert-test-local-floor.py", text)
        self.assertIn("WorkflowCommandTests", text)
        self.assertIn("workflow-regression-gate: FAIL", text)
        self.assertIn("workflow baseline promote", text)

    def test_workflow_regression_gate_workflow_runs_on_pull_request(self) -> None:
        text = (
            ROOT / ".github" / "workflows" / "workflow-regression-gate.yml"
        ).read_text(encoding="utf-8")
        self.assertIn("pull_request:", text)
        self.assertIn("scripts/workflow-regression-gate.sh", text)
        self.assertIn("scripts/lib/assert-test-local-floor.py", text)
        self.assertIn("application/src/Ashlar.CLI/**", text)

    def test_assert_test_local_floor_rejects_zero_and_accepts_positive(self) -> None:
        helper = ROOT / "scripts" / "lib" / "assert-test-local-floor.py"
        with tempfile.TemporaryDirectory() as tmp:
            empty = Path(tmp) / "empty.json"
            empty.write_text('{"TotalTests":0,"PassedTests":0,"FailedTests":0}\n', encoding="utf-8")
            zero = subprocess.run(
                [sys.executable, str(helper), str(empty)],
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertEqual(1, zero.returncode, zero.stderr)
            self.assertIn("matched 0 tests", zero.stderr)
            ok = Path(tmp) / "ok.json"
            ok.write_text('{"TotalTests":1,"PassedTests":1,"FailedTests":0}\n', encoding="utf-8")
            passed = subprocess.run(
                [sys.executable, str(helper), str(ok)],
                check=False,
                capture_output=True,
                text=True,
            )
        self.assertEqual(0, passed.returncode, passed.stderr)
        self.assertIn("TotalTests=1", passed.stdout)


class RcGateWorkflowTests(unittest.TestCase):
    def test_rc_gate_workflow_runs_tier_c_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "rc-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("pull_request:", text)
        self.assertIn("scripts/rc-gate*.sh", text)
        self.assertIn("ci release-bundle --profile quick", text)
        self.assertIn("make rc-gate-tier-c", text)
        self.assertIn("github.event_name != 'workflow_dispatch'", text)
        self.assertNotIn(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'c'",
            text,
        )

    def test_rc_gate_workflow_runs_tier_e_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "rc-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("docs/exceptions.yaml", text)
        self.assertIn("make rc-gate-tier-e", text)
        self.assertIn(
            "github.event_name != 'workflow_dispatch' || inputs.tier == 'e'",
            text,
        )
        self.assertNotIn(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'e'",
            text,
        )

    def test_rc_gate_workflow_produces_supply_chain_evidence(self) -> None:
        text = (ROOT / ".github" / "workflows" / "rc-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("make security-gate-tier-d", text)
        self.assertIn("SECURITY_GATE_STRICT_SUPPLY_CHAIN", text)
        self.assertIn("make rc-gate-tier-c", text)


class ApplicationTierCCountedApiTests(unittest.TestCase):
    def test_application_gate_tier_c_runs_counted_api_suite_on_net10(self) -> None:
        text = (ROOT / "scripts" / "application-gate-tier-c.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 4", text)
        self.assertIn("-f net10.0", text)
        self.assertNotIn("-f net8.0", text)
        self.assertNotIn('dotnet test "$INFRA"', text)

    def test_application_gate_workflow_runs_tier_c_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "application-gate.yml").read_text(encoding="utf-8")
        self.assertIn("Tests/API/**", text)
        self.assertIn("APPLICATION_GATE_STRICT_DOCTOR", text)
        self.assertNotIn(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'c'",
            text,
        )


class ApplicationTierBCountedCliTests(unittest.TestCase):
    def test_application_gate_tier_b_runs_counted_cli_suite_on_net10(self) -> None:
        text = (ROOT / "scripts" / "application-gate-tier-b.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("Ashlar.Tests.CLI", text)
        self.assertIn("--min-tests 200", text)
        self.assertIn("FullyQualifiedName!~UnitTestBridgeTests", text)
        self.assertIn("-f net10.0", text)
        self.assertNotIn("APPLICATION_GATE_STRICT_DOCTOR", text)
        self.assertNotIn('dotnet test "$CLI_TESTS"', text)

    def test_application_gate_tier_b_fails_closed_on_doctor(self) -> None:
        text = (ROOT / "scripts" / "application-gate-tier-b.sh").read_text(
            encoding="utf-8"
        )
        self.assertIn("doctor --json exited", text)
        self.assertIn("application-gate-tier-b: FAIL", text)
        self.assertNotIn("APPLICATION_GATE_STRICT_DOCTOR", text)
        self.assertNotIn("warnings may fail strict profile", text)


class KernelTierACountedTests(unittest.TestCase):
    def test_kernel_gate_tier_a_runs_counted_hosting_and_pipeline_slices(self) -> None:
        text = (ROOT / "scripts" / "kernel-gate-tier-a.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 40", text)
        self.assertIn("--min-tests 14", text)
        makefile = (ROOT / "Makefile").read_text(encoding="utf-8")
        self.assertIn("kernel-gate-tier-a.sh", makefile)
        self.assertNotIn(
            "FullyQualifiedName~KernelPhaseResolutionTests|FullyQualifiedName~HostingDeploymentProfileTests|FullyQualifiedName~HostingE2ESmokeTests",
            makefile,
        )


class KernelTierBCountedTests(unittest.TestCase):
    def test_kernel_gate_tier_b_runs_counted_pipeline_lifecycle_slice(self) -> None:
        text = (ROOT / "scripts" / "kernel-gate-tier-b.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 14", text)
        self.assertNotIn(
            'dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj -f net8.0',
            text,
        )

    def test_kernel_gate_workflow_runs_tier_b_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "kernel-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("kernel-gate-tier-b", text)
        self.assertNotIn(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'b'",
            text,
        )


class KernelTierCCountedTests(unittest.TestCase):
    def test_kernel_gate_tier_c_runs_counted_workflow_and_airgapped_slices(self) -> None:
        text = (ROOT / "scripts" / "kernel-gate-tier-c.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 12", text)
        self.assertIn("--min-tests 17", text)
        self.assertIn("WorkflowExecutorIntegrationTests", text)
        self.assertIn("FullyQualifiedName~AirGapped", text)
        self.assertIn("FullyQualifiedName!~EnrolledSuiteConventionTests", text)
        self.assertIn("-f net10.0", text)
        self.assertNotIn('dotnet test "$INFRA"', text)

    def test_kernel_gate_workflow_runs_tier_c_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "kernel-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("kernel-gate-tier-c", text)
        self.assertNotIn(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'c'",
            text,
        )


class DistributionMatrixIAshlarClientCountedTests(unittest.TestCase):
    def test_distribution_matrix_runs_counted_iashlar_client_slice(self) -> None:
        script = (
            ROOT / "scripts" / "distribution-matrix-iashlar-client.sh"
        ).read_text(encoding="utf-8")
        workflow = (
            ROOT / ".github" / "workflows" / "distribution-matrix-gate.yml"
        ).read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", script)
        self.assertIn("--min-tests 1", script)
        self.assertIn("Virtual_prod_IAshlarClient_GetStatusAsync", script)
        self.assertIn("-f net10.0", script)
        self.assertNotIn(
            "dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj",
            script,
        )
        self.assertIn("scripts/distribution-matrix-iashlar-client.sh", workflow)
        self.assertNotIn(
            "dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj",
            workflow,
        )


class KernelTierECountedTests(unittest.TestCase):
    def test_kernel_gate_tier_e_runs_counted_otel_and_performance_slices(self) -> None:
        text = (ROOT / "scripts" / "kernel-gate-tier-e.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 1", text)
        self.assertIn("--min-tests 3", text)
        self.assertIn("OpenTelemetryTests", text)
        self.assertIn("Ashlar.Tests.Orchestration.Performance", text)
        self.assertNotIn('dotnet test "$INFRA"', text)
        self.assertNotIn('dotnet test "$ORCH"', text)


class TestProdStyleCountedTests(unittest.TestCase):
    def test_makefile_runs_counted_prod_style_suite(self) -> None:
        text = (ROOT / "Makefile").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 123", text)
        self.assertIn(
            "Category=ProdStyle&FullyQualifiedName!~ForgeEndpointsTests&FullyQualifiedName!~FrameworkVirtualProdDemosTests",
            text,
        )
        self.assertNotIn(
            "ASHLAR_ALLOW_MOCK=1 dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj -f net8.0 --no-build",
            text,
        )


class ShipTierBCountedTests(unittest.TestCase):
    def test_ship_gate_tier_b_runs_counted_framework_smoke(self) -> None:
        text = (ROOT / "scripts" / "ship-gate-tier-b.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 9", text)
        self.assertIn("doctor --json exited", text)
        self.assertNotIn("SHIP_GATE_STRICT_DOCTOR", text)
        self.assertNotIn('dotnet test "$INFRA"', text)

    def test_ship_gate_tier_b_fails_closed_on_doctor(self) -> None:
        text = (ROOT / "scripts" / "ship-gate-tier-b.sh").read_text(encoding="utf-8")
        self.assertIn("doctor --json exited", text)
        self.assertIn("ship-gate-tier-b: FAIL", text)
        self.assertNotIn("SHIP_GATE_STRICT_DOCTOR", text)
        self.assertNotIn("warnings may fail strict profile", text)

    def test_ship_gate_tier_b_invokes_counted_prod_style_target(self) -> None:
        text = (ROOT / "scripts" / "ship-gate-tier-b.sh").read_text(encoding="utf-8")
        self.assertIn("make test-prod-style", text)

    def test_ship_gate_workflow_runs_tier_b_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "ship-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("pull_request:", text)
        self.assertIn("ship-gate-tier-b", text)
        self.assertIn("SHIP_GATE_STRICT_DOCTOR", text)
        self.assertNotIn(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'b'",
            text,
        )

    def test_ship_gate_workflow_runs_tier_d_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "ship-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("pull_request:", text)
        self.assertIn("scripts/ship-gate-tier-d.sh", text)
        self.assertIn("ship-gate-tier-d", text)
        self.assertIn("github.event_name != 'workflow_dispatch'", text)
        self.assertNotIn(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'd'",
            text,
        )

    def test_ship_gate_workflow_runs_tier_c_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "ship-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("pull_request:", text)
        self.assertIn("scripts/ship-gate-tier-c.sh", text)
        self.assertIn("ship-gate-tier-c", text)
        self.assertIn(
            "github.event_name != 'workflow_dispatch' || inputs.tier == 'c'",
            text,
        )
        self.assertNotIn(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'c'",
            text,
        )


class ValidateSafeCountedTests(unittest.TestCase):
    def test_validate_safe_runs_counted_framework_smoke(self) -> None:
        text = (ROOT / "scripts" / "validate-safe.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 9", text)
        self.assertIn("BaseFrameworkSmokeTests", text)
        self.assertNotIn(
            "dotnet test src/Ashlar.Tests.Infrastructure/Ashlar.Tests.Infrastructure.csproj",
            text,
        )


class ReadinessGateLocalCountedTests(unittest.TestCase):
    def test_readiness_gate_local_runs_counted_cli_suite(self) -> None:
        text = (ROOT / "scripts" / "readiness-gate-local.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 200", text)
        self.assertIn("FullyQualifiedName!~UnitTestBridgeTests", text)
        self.assertIn("application-tests-cli-full", text)
        self.assertNotIn(
            "dotnet test application/src/Ashlar.Tests.CLI/Ashlar.Tests.CLI.csproj",
            text,
        )


class SecurityTierECountedTests(unittest.TestCase):
    def test_security_gate_tier_e_runs_counted_airgapped_suite_on_net10(self) -> None:
        text = (ROOT / "scripts" / "security-gate-tier-e.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 52", text)
        self.assertIn("FullyQualifiedName!~EnrolledSuiteConventionTests", text)
        self.assertIn("-f net10.0", text)
        self.assertNotIn("-f net8.0", text)

    def test_security_gate_workflow_runs_tier_e_host_suite_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "security-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("security-gate-tier-e", text)
        self.assertNotIn(
            "github.event_name == 'workflow_dispatch' && (inputs.tier == 'e' || inputs.tier == 'full')",
            text,
        )
        self.assertNotIn("SECURITY_GATE_AIRGAPPED_CONTAINER:", text)


class ShipTierACountedTests(unittest.TestCase):
    def test_ship_gate_tier_a_runs_counted_host_di_smoke(self) -> None:
        text = (ROOT / "scripts" / "ship-gate-tier-a.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 2", text)
        self.assertNotIn('dotnet test "$INFRA_TESTS"', text)


class ShipTierDFailClosedTests(unittest.TestCase):
    def test_ship_gate_tier_d_always_runs_release_bundle(self) -> None:
        text = (ROOT / "scripts" / "ship-gate-tier-d.sh").read_text(encoding="utf-8")
        self.assertIn("ci release-bundle", text)
        self.assertIn("SHIP_GATE_BUNDLE_PROFILE", text)
        self.assertIn("ship-gate-tier-d: PASS", text)
        self.assertNotIn("SHIP_GATE_RUN_RUNTIME_GATE", text)


class OpenCoreBoundaryCensusTests(unittest.TestCase):
    def test_open_core_boundary_doc_matches_live_scan(self) -> None:
        run = subprocess.run(
            [sys.executable, str(ROOT / "scripts" / "verify-open-commercial-dependency-boundary.py")],
            cwd=ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
        )
        self.assertEqual(0, run.returncode, run.stdout)
        scan = next(
            (line.strip() for line in run.stdout.splitlines() if line.startswith("dependency-boundary: scanned ")),
            "",
        )
        self.assertTrue(scan, run.stdout)
        doc = (ROOT / "docs" / "OpenCoreBoundary.md").read_text(encoding="utf-8")
        self.assertIn(scan, doc)


class AutonomyObjectivePathTests(unittest.TestCase):
    def test_objectives_do_not_cite_removed_applications_tree(self) -> None:
        folder = ROOT / "samples" / "autonomy-objectives"
        for path in folder.glob("*.md"):
            text = path.read_text(encoding="utf-8")
            if "pathPrefixes:" not in text:
                continue
            self.assertNotIn("applications/Ashlar.Samples.Dogfood", text, path.name)
            self.assertIn("samples/dogfood/", text, path.name)


class SecurityTierACountedTests(unittest.TestCase):
    def test_security_gate_tier_a_runs_counted_trust_suite(self) -> None:
        text = (ROOT / "scripts" / "security-gate-tier-a.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 97", text)
        self.assertIn("Ashlar.Tests.Infrastructure.Tests.Trust", text)
        self.assertNotIn('dotnet test "$INFRA"', text)


class SecurityTierCCountedTests(unittest.TestCase):
    def test_security_gate_tier_c_runs_counted_cli_trust_surface(self) -> None:
        text = (ROOT / "scripts" / "security-gate-tier-c.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("--min-tests 61", text)
        self.assertIn("SafePackageReadTests", text)
        self.assertNotIn('dotnet test "$CLI_TESTS"', text)


class SecurityTierBCountedNet10Tests(unittest.TestCase):
    def test_security_gate_tier_b_runs_counted_api_suite_on_net10(self) -> None:
        text = (ROOT / "scripts" / "security-gate-tier-b.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("-f net10.0", text)
        self.assertIn("--min-tests 44", text)
        self.assertNotIn('dotnet test "$INFRA" -f net8.0', text)
        self.assertNotIn('dotnet build "$INFRA" -f net8.0', text)


class DockerTierFailClosedTests(unittest.TestCase):
    def _run_script(
        self,
        script: str,
        *,
        docker_exit: int,
        extra_env: dict[str, str] | None = None,
    ) -> subprocess.CompletedProcess[str]:
        with tempfile.TemporaryDirectory() as tmp:
            fake_bin = Path(tmp) / "bin"
            fake_bin.mkdir()
            docker = fake_bin / "docker"
            docker.write_text(
                f"#!/usr/bin/env bash\nexit {docker_exit}\n",
                encoding="utf-8",
            )
            docker.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{fake_bin}{os.pathsep}{env['PATH']}"
            env.pop("OPS_GATE_MESH_DEEP", None)
            env.pop("OPS_GATE_CHAOS_LITE", None)
            if extra_env:
                env.update(extra_env)
            return subprocess.run(
                ["bash", str(ROOT / "scripts" / script)],
                cwd=ROOT,
                env=env,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
            )

    def _run_with_dead_docker(
        self,
        script: str,
        extra_env: dict[str, str] | None = None,
    ) -> subprocess.CompletedProcess[str]:
        return self._run_script(script, docker_exit=1, extra_env=extra_env)

    def test_application_tier_d_refuses_missing_docker(self) -> None:
        run = self._run_with_dead_docker("application-gate-tier-d.sh")
        self.assertEqual(2, run.returncode)
        self.assertIn("requires a working Docker daemon", run.stdout)
        self.assertNotIn("application-gate-tier-d: PASS", run.stdout)

    def test_mesh_tier_d_refuses_missing_docker(self) -> None:
        run = self._run_with_dead_docker("composition-mesh-gate-tier-d.sh")
        self.assertEqual(2, run.returncode)
        self.assertIn("requires a working Docker daemon", run.stdout)
        self.assertNotIn("composition-mesh-gate-tier-d: PASS", run.stdout)

    def test_ops_tier_d_refuses_missing_docker(self) -> None:
        run = self._run_with_dead_docker(
            "ops-gate-tier-d.sh",
            extra_env={"OPS_GATE_MESH_DEEP": "1"},
        )
        self.assertEqual(2, run.returncode)
        self.assertIn("requires a working Docker daemon", run.stdout)
        self.assertNotIn("ops-gate-tier-d: PASS", run.stdout)

    def test_ops_tier_d_refuses_missing_proof_flags(self) -> None:
        run = self._run_script("ops-gate-tier-d.sh", docker_exit=0)
        self.assertEqual(2, run.returncode)
        self.assertIn("OPS_GATE_MESH_DEEP=1 or OPS_GATE_CHAOS_LITE=1", run.stdout)
        self.assertNotIn("ops-gate-tier-d: PASS", run.stdout)

    def test_ops_tier_d_refuses_chaos_lite_without_mesh_env(self) -> None:
        run = self._run_script(
            "ops-gate-tier-d.sh",
            docker_exit=0,
            extra_env={"OPS_GATE_CHAOS_LITE": "1"},
        )
        self.assertEqual(2, run.returncode)
        self.assertIn("chaos-lite requires .env.mesh-lab", run.stdout)
        self.assertNotIn("ops-gate-tier-d: PASS", run.stdout)

    def test_kernel_tier_e_refuses_missing_docker(self) -> None:
        run = self._run_with_dead_docker("kernel-gate-tier-e.sh")
        self.assertEqual(2, run.returncode)
        self.assertIn("requires a working Docker daemon", run.stdout)
        self.assertNotIn("kernel-gate-tier-e: PASS", run.stdout)
        self.assertNotIn("prod-dry-run skipped", run.stdout)

    def test_ops_gate_full_does_not_invoke_d_without_proof_flags(self) -> None:
        text = (ROOT / "Makefile").read_text(encoding="utf-8")
        start = text.index("ops-gate-full:")
        end = text.find("\n# Meta gate:", start)
        block = text[start:end]
        self.assertIn("OPS_GATE_MESH_DEEP", block)
        self.assertIn("OPS_GATE_CHAOS_LITE", block)
        self.assertIn("skipping D", block)

    def test_ops_gate_tier_a_runs_counted_dogfood_blocks(self) -> None:
        text = (ROOT / "scripts" / "ops-gate-tier-a.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("OPS_GATE_MIN_DOGFOOD_TESTS", text)
        self.assertIn(
            '--expected-prefix "Ashlar.Tests.Infrastructure.Tests.Dogfood.DogfoodBlock"',
            text,
        )
        self.assertIn("DogfoodBlock1Tests", text)
        self.assertIn("DogfoodBlock6Tests", text)
        self.assertIn("-f net8.0", text)
        self.assertIn("counted-dogfood-1-6", text)
        self.assertNotIn("dogfood-phase-c", text)
        self.assertNotIn('dotnet test "$INFRA"', text)

    def test_ops_gate_workflow_runs_tier_a_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "ops-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("pull_request:", text)
        self.assertIn("Tests/Dogfood/**", text)
        self.assertIn("scripts/ops-gate-tier-a.sh", text)
        self.assertIn("ops-gate-tier-a", text)
        self.assertIn(
            "github.event_name != 'workflow_dispatch' || inputs.tier == 'a'",
            text,
        )
        self.assertNotIn(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'a'",
            text,
        )

    def test_ops_gate_tier_b_runs_counted_dogfood_blocks(self) -> None:
        text = (ROOT / "scripts" / "ops-gate-tier-b.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("OPS_GATE_MIN_DOGFOOD_B_TESTS", text)
        self.assertIn(
            '--expected-prefix "Ashlar.Tests.Infrastructure.Tests.Dogfood.DogfoodBlock"',
            text,
        )
        self.assertIn("DogfoodBlock7Tests", text)
        self.assertIn("DogfoodBlock9LocalIpcTests", text)
        self.assertIn("-f net8.0", text)
        self.assertIn("counted-dogfood-7-9-ipc", text)
        self.assertNotIn("dogfood-phase-de", text)
        self.assertNotIn("dogfood-block9-ipc", text)
        self.assertNotIn('dotnet test "$INFRA"', text)

    def test_ops_gate_workflow_runs_tier_b_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "ops-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("pull_request:", text)
        self.assertIn("scripts/ops-gate-tier-b.sh", text)
        self.assertIn("ops-gate-tier-b", text)
        self.assertIn(
            "github.event_name != 'workflow_dispatch' || inputs.tier == 'b'",
            text,
        )
        self.assertNotIn(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'b'",
            text,
        )

    def test_ops_gate_tier_c_runs_counted_closed_loop(self) -> None:
        text = (ROOT / "scripts" / "ops-gate-tier-c.sh").read_text(encoding="utf-8")
        self.assertIn("run-dotnet-test-counted.py", text)
        self.assertIn("OPS_GATE_MIN_CLOSEDLOOP_TESTS", text)
        self.assertIn("OPS_GATE_MIN_PHASE_F_TESTS", text)
        self.assertIn("OPS_GATE_RUN_PHASE_F", text)
        self.assertIn(
            '--expected-prefix "Ashlar.Tests.Infrastructure.Tests.Dogfood.DogfoodClosedLoopTests."',
            text,
        )
        self.assertIn("DogfoodPhaseFTests", text)
        self.assertIn("-f net8.0", text)
        self.assertIn("counted-closed-loop", text)
        self.assertNotIn("dogfood-closedloop", text)
        self.assertNotIn("dogfood-phasef", text)
        self.assertNotIn('dotnet test "$INFRA"', text)

    def test_ops_gate_workflow_runs_tier_c_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "ops-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("pull_request:", text)
        self.assertIn("scripts/ops-gate-tier-c.sh", text)
        self.assertIn("ops-gate-tier-c", text)
        self.assertIn(
            "github.event_name != 'workflow_dispatch' || inputs.tier == 'c'",
            text,
        )
        self.assertNotIn(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'c'",
            text,
        )

    def test_ops_gate_tier_e_runs_oh_shit_demo_quick(self) -> None:
        text = (ROOT / "scripts" / "ops-gate-tier-e.sh").read_text(encoding="utf-8")
        self.assertIn("bash scripts/oh-shit-demo.sh --quick", text)
        self.assertIn("ops-gate-tier-e: PASS", text)
        self.assertNotIn("oh-shit-demo.sh --no-build", text)

    def test_ops_gate_workflow_runs_tier_e_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "ops-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("pull_request:", text)
        self.assertIn("scripts/ops-gate-tier-e.sh", text)
        self.assertIn("scripts/oh-shit-demo.sh", text)
        self.assertIn("ops-gate-tier-e", text)
        self.assertIn(
            "github.event_name != 'workflow_dispatch' || inputs.tier == 'e'",
            text,
        )
        self.assertNotIn(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'e'",
            text,
        )


class RcTierDFailClosedTests(unittest.TestCase):
    def _run_tier_d(
        self,
        fake_bin: Path,
        extra_env: dict[str, str] | None = None,
    ) -> subprocess.CompletedProcess[str]:
        for tool in ("mkdir", "dirname"):
            resolved = shutil.which(tool)
            self.assertIsNotNone(resolved, tool)
            (fake_bin / tool).symlink_to(resolved)
        env = os.environ.copy()
        env["PATH"] = str(fake_bin)
        env["RC_GATE_GH_BRANCH"] = "master"
        env.pop("RC_GATE_GH_ADVISORY_ONLY", None)
        env.pop("RC_GATE_TRIGGER_GH", None)
        env.pop("GH_TOKEN", None)
        env.pop("GH_ENTERPRISE_TOKEN", None)
        if extra_env:
            env.update(extra_env)
        bash = shutil.which("bash")
        self.assertIsNotNone(bash)
        return subprocess.run(
            [bash, str(ROOT / "scripts" / "rc-gate-tier-d.sh")],
            cwd=ROOT,
            env=env,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
        )

    def test_rc_tier_d_refuses_missing_gh(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            fake_bin = Path(tmp) / "bin"
            fake_bin.mkdir()
            run = self._run_tier_d(fake_bin)
        self.assertEqual(2, run.returncode)
        self.assertIn("requires the GitHub CLI", run.stdout)
        self.assertNotIn("rc-gate-tier-d: PASS", run.stdout)

    def test_rc_tier_d_refuses_unauthenticated_gh(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            fake_bin = Path(tmp) / "bin"
            fake_bin.mkdir()
            gh = fake_bin / "gh"
            gh.write_text(
                "#!/usr/bin/env bash\n"
                "if [[ \"${1:-}\" == \"auth\" ]]; then exit 1; fi\n"
                "exit 0\n",
                encoding="utf-8",
            )
            gh.chmod(0o755)
            run = self._run_tier_d(fake_bin)
        self.assertEqual(2, run.returncode)
        self.assertIn("requires an authenticated GitHub CLI", run.stdout)
        self.assertNotIn("rc-gate-tier-d: PASS", run.stdout)

    def test_rc_tier_d_script_refuses_advisory_skip(self) -> None:
        text = (ROOT / "scripts" / "rc-gate-tier-d.sh").read_text(encoding="utf-8")
        self.assertIn("RC_GATE_GH_ADVISORY_ONLY is refused", text)
        self.assertIn("red workflows are a blocker", text)
        self.assertIn("rc-gate-tier-d: FAIL", text)
        self.assertNotIn("rc-gate-tier-d: PASS (advisory)", text)
        perf = (ROOT / "scripts" / "perf-gate.sh").read_text(encoding="utf-8")
        self.assertNotIn("RC_GATE_GH_ADVISORY_ONLY", perf)

    def test_rc_tier_d_refuses_advisory_only_flag(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            fake_bin = Path(tmp) / "bin"
            fake_bin.mkdir()
            run = self._run_tier_d(
                fake_bin,
                extra_env={"RC_GATE_GH_ADVISORY_ONLY": "1"},
            )
        self.assertEqual(2, run.returncode)
        self.assertIn("RC_GATE_GH_ADVISORY_ONLY is refused", run.stdout)
        self.assertNotIn("rc-gate-tier-d: PASS", run.stdout)


class RcTierCFailClosedTests(unittest.TestCase):
    def _run_c(
        self,
        bundle: Path | None,
        *,
        vuln: Path | None = None,
    ) -> subprocess.CompletedProcess[str]:
        env = os.environ.copy()
        env["RC_GATE_BUNDLE_JSON"] = (
            str(bundle) if bundle is not None else "/nonexistent/release-bundle-report.json"
        )
        env["RC_GATE_VULN_REPORT"] = (
            str(vuln) if vuln is not None else "/nonexistent/vulnerable-packages.txt"
        )
        return subprocess.run(
            ["bash", str(ROOT / "scripts" / "rc-gate-tier-c.sh")],
            cwd=ROOT,
            env=env,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
        )

    def _clean_vuln(self, directory: Path) -> Path:
        path = directory / "vulnerable-packages.txt"
        path.write_text("--- application/Ashlar.Application.sln ---\n", encoding="utf-8")
        return path

    def test_rc_tier_c_fails_when_bundle_missing(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            run = self._run_c(None, vuln=self._clean_vuln(Path(tmp)))
        self.assertEqual(1, run.returncode)
        self.assertIn("release-bundle: missing", run.stdout)
        self.assertIn("rc-gate-tier-c: FAIL", run.stdout)
        self.assertNotIn("rc-gate-tier-c: PASS", run.stdout)

    def test_rc_tier_c_fails_when_bundle_verdict_is_not_pass(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            bundle = Path(tmp) / "release-bundle-report.json"
            bundle.write_text('{"verdict": "FAIL"}\n', encoding="utf-8")
            run = self._run_c(bundle, vuln=self._clean_vuln(Path(tmp)))
        self.assertEqual(1, run.returncode)
        self.assertIn("release-bundle: FAIL", run.stdout)
        self.assertIn("rc-gate-tier-c: FAIL", run.stdout)
        self.assertNotIn("rc-gate-tier-c: PASS", run.stdout)

    def test_rc_tier_c_passes_when_bundle_verdict_is_pass(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            bundle = Path(tmp) / "release-bundle-report.json"
            bundle.write_text('{"verdict": "PASS"}\n', encoding="utf-8")
            run = self._run_c(bundle, vuln=self._clean_vuln(Path(tmp)))
        self.assertEqual(0, run.returncode)
        self.assertIn("release-bundle: PASS", run.stdout)
        self.assertIn("rc-gate-tier-c: PASS", run.stdout)

    def test_rc_tier_c_script_fails_closed_on_security_evidence(self) -> None:
        text = (ROOT / "scripts" / "rc-gate-tier-c.sh").read_text(encoding="utf-8")
        self.assertIn("security: no vulnerable-packages report", text)
        self.assertIn("security: High/Critical CVEs detected", text)
        self.assertNotIn("RC_GATE_STRICT_SECURITY", text)
        plan = (ROOT / "docs" / "production-readiness" / "RCHardeningPlan-v1.md").read_text(
            encoding="utf-8"
        )
        self.assertNotIn("RC_GATE_STRICT_SECURITY", plan)

    def test_rc_tier_c_fails_when_vuln_report_missing(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            bundle = Path(tmp) / "release-bundle-report.json"
            bundle.write_text('{"verdict": "PASS"}\n', encoding="utf-8")
            run = self._run_c(bundle, vuln=None)
        self.assertEqual(1, run.returncode)
        self.assertIn("security: no vulnerable-packages report", run.stdout)
        self.assertIn("rc-gate-tier-c: FAIL", run.stdout)
        self.assertNotIn("rc-gate-tier-c: PASS", run.stdout)

    def test_rc_tier_c_fails_when_high_critical_cves_present(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            bundle = Path(tmp) / "release-bundle-report.json"
            bundle.write_text('{"verdict": "PASS"}\n', encoding="utf-8")
            vuln = Path(tmp) / "vulnerable-packages.txt"
            vuln.write_text("Severity: High\n", encoding="utf-8")
            run = self._run_c(bundle, vuln=vuln)
        self.assertEqual(1, run.returncode)
        self.assertIn("security: High/Critical CVEs detected", run.stdout)
        self.assertIn("rc-gate-tier-c: FAIL", run.stdout)
        self.assertNotIn("rc-gate-tier-c: PASS", run.stdout)


class RcTierEFailClosedTests(unittest.TestCase):
    def _run_e(self, exceptions: Path | None) -> subprocess.CompletedProcess[str]:
        env = os.environ.copy()
        env["RC_EXCEPTIONS_FILE"] = (
            str(exceptions) if exceptions is not None else "/nonexistent/exceptions.yaml"
        )
        return subprocess.run(
            ["bash", str(ROOT / "scripts" / "rc-gate-tier-e.sh")],
            cwd=ROOT,
            env=env,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
        )

    def test_rc_tier_e_script_fails_closed_on_exceptions_policy(self) -> None:
        text = (ROOT / "scripts" / "rc-gate-tier-e.sh").read_text(encoding="utf-8")
        self.assertIn("error: exceptions: missing", text)
        self.assertIn("error: exceptions: policy validation failed", text)
        self.assertIn("rc-gate-tier-e: FAIL", text)
        self.assertNotIn("RC_GATE_STRICT_EXCEPTIONS", text)
        self.assertNotIn("policy validation failed (non-strict)", text)
        exceptions = (ROOT / "docs" / "exceptions.yaml").read_text(encoding="utf-8")
        self.assertNotIn("RC_GATE_STRICT_EXCEPTIONS", exceptions)

    def test_rc_tier_e_fails_when_exceptions_file_missing(self) -> None:
        run = self._run_e(None)
        self.assertEqual(1, run.returncode)
        self.assertIn("exceptions: missing", run.stdout)
        self.assertIn("rc-gate-tier-e: FAIL", run.stdout)
        self.assertNotIn("rc-gate-tier-e: PASS", run.stdout)

    def test_rc_tier_e_fails_when_high_critical_policy_violated(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "exceptions.yaml"
            path.write_text(
                "exceptions:\n"
                "  - id: TEST-1\n"
                "    severity: High\n",
                encoding="utf-8",
            )
            run = self._run_e(path)
        self.assertEqual(1, run.returncode)
        self.assertIn("exceptions BLOCK:", run.stdout)
        self.assertIn("policy validation failed", run.stdout)
        self.assertIn("rc-gate-tier-e: FAIL", run.stdout)
        self.assertNotIn("rc-gate-tier-e: PASS", run.stdout)

    def test_rc_tier_e_passes_on_committed_exceptions_file(self) -> None:
        run = self._run_e(ROOT / "docs" / "exceptions.yaml")
        self.assertEqual(0, run.returncode)
        self.assertIn("High/Critical policy OK", run.stdout)
        self.assertIn("rc-gate-tier-e: PASS", run.stdout)
        self.assertNotIn("rc-gate-tier-e: FAIL", run.stdout)


class SecurityTierDFailClosedTests(unittest.TestCase):
    def test_security_tier_d_fails_when_scan_cannot_run(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            fake_bin = Path(tmp) / "bin"
            fake_bin.mkdir()
            dotnet = fake_bin / "dotnet"
            dotnet.write_text(
                "#!/usr/bin/env bash\n"
                "if [[ \"$*\" == *list* ]]; then\n"
                "  echo 'error: simulated list failure' >&2\n"
                "  exit 1\n"
                "fi\n"
                "exit 0\n",
                encoding="utf-8",
            )
            dotnet.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{fake_bin}{os.pathsep}{env['PATH']}"
            env.pop("SECURITY_GATE_STRICT_SUPPLY_CHAIN", None)
            run = subprocess.run(
                ["bash", str(ROOT / "scripts" / "security-gate-tier-d.sh")],
                cwd=ROOT,
                env=env,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
            )
        self.assertEqual(1, run.returncode)
        self.assertIn("supply-chain scan could not run", run.stdout)
        self.assertIn("security-gate-tier-d: FAIL", run.stdout)
        self.assertNotIn("security-gate-tier-d: PASS", run.stdout)

    def test_security_tier_d_script_fails_closed_on_vulnerable_packages(self) -> None:
        text = (ROOT / "scripts" / "security-gate-tier-d.sh").read_text(encoding="utf-8")
        self.assertIn("Vulnerable packages detected", text)
        self.assertIn("security-gate-tier-d: FAIL", text)
        self.assertNotIn("SECURITY_GATE_STRICT_SUPPLY_CHAIN", text)
        self.assertNotIn("set SECURITY_GATE_STRICT_SUPPLY_CHAIN=1 to fail", text)

    def test_security_tier_d_fails_when_vulnerable_packages_are_reported(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            fake_bin = Path(tmp) / "bin"
            fake_bin.mkdir()
            dotnet = fake_bin / "dotnet"
            dotnet.write_text(
                "#!/usr/bin/env bash\n"
                "if [[ \"$*\" == *--vulnerable* ]]; then\n"
                "  echo 'The following packages have the following vulnerable'\n"
                "  echo 'Severity: High'\n"
                "  exit 0\n"
                "fi\n"
                "exit 0\n",
                encoding="utf-8",
            )
            dotnet.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{fake_bin}{os.pathsep}{env['PATH']}"
            env.pop("SECURITY_GATE_STRICT_SUPPLY_CHAIN", None)
            run = subprocess.run(
                ["bash", str(ROOT / "scripts" / "security-gate-tier-d.sh")],
                cwd=ROOT,
                env=env,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
            )
        self.assertEqual(1, run.returncode)
        self.assertIn("Vulnerable packages detected", run.stdout)
        self.assertIn("security-gate-tier-d: FAIL", run.stdout)
        self.assertNotIn("security-gate-tier-d: PASS", run.stdout)

    def test_security_gate_workflow_runs_tier_d_on_pull_request(self) -> None:
        text = (ROOT / ".github" / "workflows" / "security-gate.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn(
            "github.event_name != 'workflow_dispatch' || inputs.tier == 'd'",
            text,
        )
        self.assertIn('SECURITY_GATE_STRICT_SUPPLY_CHAIN: "1"', text)
        self.assertNotIn(
            "github.event_name == 'workflow_dispatch' && inputs.tier == 'd'",
            text,
        )


class SecurityTierEFailClosedTests(unittest.TestCase):
    def test_airgapped_container_refuses_missing_docker(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            fake_bin = Path(tmp) / "bin"
            fake_bin.mkdir()
            docker = fake_bin / "docker"
            docker.write_text("#!/usr/bin/env bash\nexit 1\n", encoding="utf-8")
            docker.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{fake_bin}{os.pathsep}{env['PATH']}"
            env["SECURITY_GATE_AIRGAPPED_CONTAINER"] = "1"
            run = subprocess.run(
                ["bash", str(ROOT / "scripts" / "security-gate-tier-e.sh")],
                cwd=ROOT,
                env=env,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
            )
        self.assertNotEqual(0, run.returncode)
        self.assertIn("requires a working Docker daemon", run.stdout)


class PerfBaselineFailClosedTests(unittest.TestCase):
    def _run(self, report_dir: Path, extra_env: dict[str, str] | None = None) -> subprocess.CompletedProcess[str]:
        env = os.environ.copy()
        env["PERF_GATE_REPORT_DIR"] = str(report_dir)
        env.pop("PERF_GATE_STRICT_BASELINE", None)
        env.pop("PERF_GATE_UPDATE_BASELINE", None)
        if extra_env:
            env.update(extra_env)
        return subprocess.run(
            ["bash", str(ROOT / "scripts" / "perf-gate-baseline.sh")],
            cwd=ROOT,
            env=env,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
        )

    def _write_throughput(self, report_dir: Path, total_ms: int) -> None:
        report_dir.mkdir(parents=True, exist_ok=True)
        (report_dir / "pipeline-throughput.json").write_text(
            json.dumps({"totalMs": total_ms, "avgMsPerRun": total_ms}) + "\n",
            encoding="utf-8",
        )

    def test_perf_baseline_script_fails_closed_on_regression(self) -> None:
        text = (ROOT / "scripts" / "perf-gate-baseline.sh").read_text(encoding="utf-8")
        self.assertIn("perf regression:", text)
        self.assertIn("perf-gate-baseline: FAIL", text)
        self.assertNotIn("PERF_GATE_STRICT_BASELINE", text)
        self.assertNotIn("perf regression (advisory)", text)
        plan = (ROOT / "docs" / "production-readiness" / "PerfHardeningPlan-v1.md").read_text(
            encoding="utf-8"
        )
        self.assertNotIn("PERF_GATE_STRICT_BASELINE", plan)

    def test_perf_baseline_initializes_then_fails_on_regression(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            report_dir = Path(tmp)
            self._write_throughput(report_dir, 100)
            init = self._run(report_dir, extra_env={"PERF_GATE_INIT_BASELINE": "1"})
            self.assertEqual(0, init.returncode)
            self.assertIn("perf baseline: initialized", init.stdout)
            self.assertIn("perf-gate-baseline: PASS", init.stdout)
            self._write_throughput(report_dir, 200)
            regress = self._run(report_dir)
        self.assertEqual(1, regress.returncode)
        self.assertIn("perf regression:", regress.stdout)
        self.assertIn("perf-gate-baseline: FAIL", regress.stdout)
        self.assertNotIn("perf-gate-baseline: PASS", regress.stdout)

    def test_perf_baseline_fails_when_missing_and_init_disabled(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            report_dir = Path(tmp)
            self._write_throughput(report_dir, 100)
            run = self._run(report_dir, extra_env={"PERF_GATE_INIT_BASELINE": "0"})
        self.assertEqual(1, run.returncode)
        self.assertIn("perf baseline: missing", run.stdout)
        self.assertIn("perf-gate-baseline: FAIL", run.stdout)
        self.assertNotIn("perf-gate-baseline: PASS", run.stdout)


if __name__ == "__main__":
    unittest.main()
