from __future__ import annotations

import concurrent.futures
import collections
import importlib.util
import json
import os
import subprocess
import sys
import tempfile
import threading
import time
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "autonomous-release-manager.py"


def load_script(name: str, path: Path) -> object:
    spec = importlib.util.spec_from_file_location(name, path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


arm = load_script("autonomous_release_manager", SCRIPT)
counted = load_script(
    "run_dotnet_test_counted",
    SCRIPT.parent / "run-dotnet-test-counted.py",
)
vulnerabilities = load_script(
    "verify_no_vulnerable_packages",
    SCRIPT.parent / "verify-no-vulnerable-packages.py",
)


def plan_document() -> dict:
    return {
        "schemaVersion": 1,
        "maxParallel": 2,
        "lanes": [
            {
                "id": lane_id,
                "title": lane_id.title(),
                "objective": f"Audit {lane_id}.",
                "required": True,
                "timeoutSeconds": 30,
                "steps": [
                    {
                        "name": "probe",
                        "command": [sys.executable, "-c", "print('ok')"],
                        "timeoutSeconds": 10,
                    }
                ],
            }
            for lane_id in sorted(arm.CANONICAL_LANES)
        ],
    }


class PlanValidationTests(unittest.TestCase):
    def write_plan(self, root: Path, document: dict) -> Path:
        path = root / "plan.json"
        path.write_text(json.dumps(document), encoding="utf-8")
        return path

    def test_valid_plan_requires_all_six_release_blocking_lanes(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            plan = arm.load_plan(self.write_plan(Path(temp), plan_document()))

        self.assertEqual(arm.CANONICAL_LANES, {lane.lane_id for lane in plan.lanes})
        self.assertTrue(all(lane.required for lane in plan.lanes))

    def test_missing_lane_fails_closed(self) -> None:
        document = plan_document()
        document["lanes"] = [
            lane for lane in document["lanes"] if lane["id"] != "security"
        ]
        with tempfile.TemporaryDirectory() as temp:
            with self.assertRaisesRegex(arm.PlanError, r"Missing=\['security'\]"):
                arm.load_plan(self.write_plan(Path(temp), document))

    def test_optional_canonical_lane_is_rejected(self) -> None:
        document = plan_document()
        document["lanes"][0]["required"] = False
        with tempfile.TemporaryDirectory() as temp:
            with self.assertRaisesRegex(arm.PlanError, "must be release-blocking"):
                arm.load_plan(self.write_plan(Path(temp), document))

    def test_publish_command_is_rejected(self) -> None:
        document = plan_document()
        document["lanes"][0]["steps"][0]["command"] = [
            "dotnet",
            "nuget",
            "push",
            "artifact.nupkg",
        ]
        with tempfile.TemporaryDirectory() as temp:
            with self.assertRaisesRegex(arm.PlanError, "publishing"):
                arm.load_plan(self.write_plan(Path(temp), document))

    def test_inline_shell_code_is_rejected(self) -> None:
        document = plan_document()
        document["lanes"][0]["steps"][0]["command"] = ["bash", "-c", "echo ok"]
        with tempfile.TemporaryDirectory() as temp:
            with self.assertRaisesRegex(arm.PlanError, "inline shell code"):
                arm.load_plan(self.write_plan(Path(temp), document))

    def test_reserved_step_environment_is_rejected(self) -> None:
        document = plan_document()
        document["lanes"][0]["steps"][0]["environment"] = {"PATH": "/tmp/evil"}
        with tempfile.TemporaryDirectory() as temp:
            with self.assertRaisesRegex(arm.PlanError, "reserved variable PATH"):
                arm.load_plan(self.write_plan(Path(temp), document))

    def test_parallelism_cannot_exceed_lane_count(self) -> None:
        document = plan_document()
        document["maxParallel"] = 7
        with tempfile.TemporaryDirectory() as temp:
            with self.assertRaisesRegex(arm.PlanError, "cannot exceed 6"):
                arm.load_plan(self.write_plan(Path(temp), document))

    def test_step_budgets_must_fit_lane_timeout(self) -> None:
        document = plan_document()
        document["lanes"][0]["timeoutSeconds"] = 5
        with tempfile.TemporaryDirectory() as temp:
            with self.assertRaisesRegex(arm.PlanError, "exceeding lane timeout"):
                arm.load_plan(self.write_plan(Path(temp), document))

    def test_committed_semantic_agents_and_skill_are_complete(self) -> None:
        arm.validate_cursor_assets(SCRIPT.parents[1])

    def test_skill_frontmatter_allows_path_lists(self) -> None:
        skill = SCRIPT.parents[1] / ".cursor" / "skills" / "release-manager" / "SKILL.md"
        metadata, body = arm._frontmatter(skill)
        self.assertEqual("release-manager", metadata["name"])
        self.assertIn("code-auditor", body)
        self.assertIn("paths", metadata)

    def test_canonical_plan_is_bound_to_head_and_code_digest(self) -> None:
        repo = SCRIPT.parents[1]
        sha = subprocess.check_output(
            ["git", "rev-parse", "HEAD"], cwd=repo, text=True
        ).strip()
        digest = arm.validate_plan_binding(
            repo, repo / arm.CANONICAL_PLAN_RELATIVE_PATH, sha
        )
        self.assertEqual(arm.TRUSTED_PLAN_SHA256, digest)
        self.assertEqual(64, len(arm.validate_coordinator_binding(repo, sha)))

    def test_untrusted_absolute_executable_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            fake = Path(temp) / "evil-python"
            fake.write_text("#!/usr/bin/env sh\nexit 0\n", encoding="utf-8")
            fake.chmod(0o755)
            with self.assertRaisesRegex(arm.PlanError, "outside the trusted PATH"):
                arm._trusted_executable(str(fake))

    def test_coordinator_git_ignores_inherited_path(self) -> None:
        repo = SCRIPT.parents[1]
        with tempfile.TemporaryDirectory() as temp:
            sentinel = Path(temp) / "fake-git-ran"
            fake_git = Path(temp) / "git"
            fake_git.write_text(
                f"#!/usr/bin/env sh\ntouch {str(sentinel)!r}\nexit 1\n",
                encoding="utf-8",
            )
            fake_git.chmod(0o755)
            previous = os.environ.get("PATH", "")
            os.environ["PATH"] = f"{temp}{os.pathsep}{previous}"
            try:
                head = arm._git(repo, "rev-parse", "HEAD")
            finally:
                os.environ["PATH"] = previous
            self.assertEqual(40, len(head))
            self.assertFalse(sentinel.exists())

    def test_missing_semantic_agents_fail_validation(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            with self.assertRaisesRegex(arm.PlanError, "Required Cursor asset is missing"):
                arm.validate_cursor_assets(Path(temp))


class ExecutionTests(unittest.TestCase):
    def test_lane_collects_all_steps_and_fails_on_error_and_timeout(self) -> None:
        lane = arm.LaneSpec(
            lane_id="tests",
            title="Tests",
            objective="Exercise process outcomes.",
            required=True,
            timeout_seconds=10,
            exclusive_resources=(),
            steps=(
                arm.StepSpec(
                    "pass",
                    (sys.executable, "-c", "print('pass')"),
                    3,
                    {},
                    ".",
                ),
                arm.StepSpec(
                    "fail",
                    (sys.executable, "-c", "raise SystemExit(7)"),
                    3,
                    {},
                    ".",
                ),
                arm.StepSpec(
                    "timeout",
                    (sys.executable, "-c", "import time; time.sleep(5)"),
                    1,
                    {},
                    ".",
                ),
            ),
        )
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            output = root / "output"
            result = arm.run_lane(
                lane,
                root,
                {"version": "0.2.0", "sha": "a" * 40, "workspace": str(root)},
                output,
            )

            self.assertEqual("failed", result.status)
            self.assertEqual(["passed", "failed", "timeout"], [s.status for s in result.steps])
            self.assertEqual(7, result.steps[1].exit_code)
            self.assertEqual(64, len(result.log_sha256))
            self.assertTrue((output / result.log_path).exists())

    def test_report_is_blocked_by_repository_finding_even_when_lanes_pass(self) -> None:
        result = arm.LaneResult(
            lane_id="code",
            title="Code",
            objective="Audit code.",
            required=True,
            status="passed",
            duration_seconds=1,
            log_path="logs/code.log",
            log_sha256="0" * 64,
            steps=(),
        )
        with tempfile.TemporaryDirectory() as temp:
            _, markdown, verdict = arm.write_reports(
                output_directory=Path(temp),
                run_id="run",
                started_at="2026-09-05T00:00:00+00:00",
                completed_at="2026-09-05T00:00:01+00:00",
                sha="a" * 40,
                version="0.2.0",
                plan_sha256="1" * 64,
                coordinator_sha256="2" * 64,
                findings=[
                    {
                        "id": "blocker",
                        "severity": "blocker",
                        "message": "Not ready.",
                    }
                ],
                lane_results=[result],
            )

            self.assertEqual("blocked", verdict)
            self.assertIn("**BLOCKED**", markdown.read_text(encoding="utf-8"))

    def test_intentional_skip_does_not_add_incomplete_lanes(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            json_path, _, verdict = arm.write_reports(
                output_directory=Path(temp),
                run_id="run",
                started_at="2026-09-05T00:00:00+00:00",
                completed_at="2026-09-05T00:00:01+00:00",
                sha="a" * 40,
                version="0.1.2",
                plan_sha256="1" * 64,
                coordinator_sha256="2" * 64,
                findings=[
                    {
                        "id": "candidate-version-not-advanced",
                        "severity": "blocker",
                        "message": "VERSION is not a candidate.",
                    },
                    {
                        "id": "commands-not-started",
                        "severity": "info",
                        "message": "Repository state is unsafe; audit commands were not started.",
                    },
                ],
                lane_results=[],
            )
            report = json.loads(json_path.read_text(encoding="utf-8"))
        self.assertEqual("blocked", verdict)
        self.assertNotIn(
            "incomplete-lane-results",
            {finding["id"] for finding in report["repositoryFindings"]},
        )

    def test_duplicate_lane_results_cannot_produce_ready(self) -> None:
        results = [
            arm.LaneResult(
                lane_id=lane_id,
                title=lane_id,
                objective="Audit.",
                required=True,
                status="passed",
                duration_seconds=1,
                log_path=f"logs/{lane_id}.log",
                log_sha256="0" * 64,
                steps=(),
            )
            for lane_id in sorted(arm.CANONICAL_LANES)
        ]
        results.append(results[0])
        with tempfile.TemporaryDirectory() as temp:
            json_path, _, verdict = arm.write_reports(
                output_directory=Path(temp),
                run_id="run",
                started_at="2026-09-05T00:00:00+00:00",
                completed_at="2026-09-05T00:00:01+00:00",
                sha="a" * 40,
                version="0.2.0",
                plan_sha256="1" * 64,
                coordinator_sha256="2" * 64,
                findings=[],
                lane_results=results,
            )
            report = json.loads(json_path.read_text(encoding="utf-8"))
        self.assertEqual("blocked", verdict)
        self.assertIn(
            "incomplete-lane-results",
            {finding["id"] for finding in report["repositoryFindings"]},
        )

    def test_lanes_with_same_host_resource_are_serialized(self) -> None:
        def lane(lane_id: str) -> object:
            return arm.LaneSpec(
                lane_id=lane_id,
                title=lane_id,
                objective="Exercise exclusive resource lock.",
                required=True,
                timeout_seconds=5,
                exclusive_resources=("docker",),
                steps=(
                    arm.StepSpec(
                        "sleep",
                        (sys.executable, "-c", "import time; time.sleep(0.25)"),
                        2,
                        {},
                        ".",
                    ),
                ),
            )

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            context = {"version": "0.2.0", "sha": "a" * 40, "workspace": str(root)}
            locks = {"docker": threading.Lock()}
            started = time.monotonic()
            with concurrent.futures.ThreadPoolExecutor(max_workers=2) as executor:
                results = list(
                    executor.map(
                        lambda spec: arm.run_lane_with_resources(
                            spec, root, context, root / "output", locks
                        ),
                        [lane("security"), lane("operations")],
                    )
                )
            elapsed = time.monotonic() - started

        self.assertTrue(all(result.status == "passed" for result in results))
        self.assertGreaterEqual(elapsed, 0.45)

    def test_timeout_kills_descendants_before_log_hash_is_final(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            sentinel = root / "descendant-survived"
            child_code = (
                "import pathlib,signal,time;"
                "signal.signal(signal.SIGTERM, signal.SIG_IGN);"
                "time.sleep(2);"
                f"pathlib.Path({str(sentinel)!r}).write_text('survived')"
            )
            parent_code = (
                "import subprocess,sys,time;"
                f"subprocess.Popen([{sys.executable!r}, '-c', {child_code!r}], start_new_session=True);"
                "time.sleep(10)"
            )
            lane = arm.LaneSpec(
                lane_id="tests",
                title="Timeout",
                objective="Prove child containment.",
                required=True,
                timeout_seconds=4,
                exclusive_resources=(),
                steps=(
                    arm.StepSpec(
                        "timeout",
                        (sys.executable, "-c", parent_code),
                        1,
                        {},
                        ".",
                    ),
                ),
            )
            result = arm.run_lane(
                lane,
                root,
                {"version": "0.2.0", "sha": "a" * 40, "workspace": str(root)},
                root / "output",
            )
            digest_at_return = result.log_sha256
            time.sleep(2.2)
            digest_later = arm.hashlib.sha256(
                (root / "output" / result.log_path).read_bytes()
            ).hexdigest()

            self.assertEqual("failed", result.status)
            self.assertEqual("timeout", result.steps[0].status)
            self.assertFalse(sentinel.exists())
            self.assertEqual(digest_at_return, digest_later)

    def test_inherited_bash_env_cannot_inject_step_code(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            sentinel = root / "bash-env-ran"
            injected = root / "injected.sh"
            injected.write_text(f"touch {sentinel}\n", encoding="utf-8")
            probe = root / "probe.sh"
            probe.write_text("#!/usr/bin/env bash\nexit 0\n", encoding="utf-8")
            probe.chmod(0o755)
            lane = arm.LaneSpec(
                lane_id="security",
                title="Environment",
                objective="Reject inherited shell hooks.",
                required=True,
                timeout_seconds=5,
                exclusive_resources=(),
                steps=(arm.StepSpec("probe", ("bash", "probe.sh"), 2, {}, "."),),
            )
            previous = os.environ.get("BASH_ENV")
            os.environ["BASH_ENV"] = str(injected)
            try:
                result = arm.run_lane(
                    lane,
                    root,
                    {"version": "0.2.0", "sha": "a" * 40, "workspace": str(root)},
                    root / "output",
                )
            finally:
                if previous is None:
                    os.environ.pop("BASH_ENV", None)
                else:
                    os.environ["BASH_ENV"] = previous

            self.assertEqual("passed", result.status)
            self.assertFalse(sentinel.exists())

    def test_inherited_dotnet_root_cannot_replace_trusted_executable(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            sentinel = root / "fake-bash-ran"
            fake_bash = root / "bash"
            fake_bash.write_text(
                f"#!/usr/bin/env sh\ntouch {str(sentinel)!r}\nexit 0\n",
                encoding="utf-8",
            )
            fake_bash.chmod(0o755)
            probe = root / "probe.sh"
            probe.write_text("#!/usr/bin/env bash\nexit 0\n", encoding="utf-8")
            lane = arm.LaneSpec(
                lane_id="security",
                title="Trusted path",
                objective="Reject DOTNET_ROOT executable injection.",
                required=True,
                timeout_seconds=5,
                exclusive_resources=(),
                steps=(arm.StepSpec("probe", ("bash", "probe.sh"), 2, {}, "."),),
            )
            previous = os.environ.get("DOTNET_ROOT")
            os.environ["DOTNET_ROOT"] = str(root)
            try:
                result = arm.run_lane(
                    lane,
                    root,
                    {"version": "0.2.0", "sha": "a" * 40, "workspace": str(root)},
                    root / "output",
                )
            finally:
                if previous is None:
                    os.environ.pop("DOTNET_ROOT", None)
                else:
                    os.environ["DOTNET_ROOT"] = previous

            self.assertEqual("passed", result.status)
            self.assertFalse(sentinel.exists())

    def test_step_environment_cannot_override_trusted_path(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            sentinel = root / "fake-bash-ran"
            fake_bin = root / "evil"
            fake_bin.mkdir()
            fake_bash = fake_bin / "bash"
            fake_bash.write_text(
                f"#!/usr/bin/env sh\ntouch {str(sentinel)!r}\nexit 0\n",
                encoding="utf-8",
            )
            fake_bash.chmod(0o755)
            probe = root / "probe.sh"
            probe.write_text("#!/usr/bin/env bash\nexit 0\n", encoding="utf-8")
            probe.chmod(0o755)
            lane = arm.LaneSpec(
                lane_id="security",
                title="Reserved env",
                objective="Reject plan PATH overrides.",
                required=True,
                timeout_seconds=5,
                exclusive_resources=(),
                steps=(
                    arm.StepSpec(
                        "probe",
                        ("bash", "probe.sh"),
                        2,
                        {"PATH": str(fake_bin)},
                        ".",
                    ),
                ),
            )
            result = arm.run_lane(
                lane,
                root,
                {"version": "0.2.0", "sha": "a" * 40, "workspace": str(root)},
                root / "output",
            )
            self.assertEqual("failed", result.status)
            self.assertEqual("error", result.steps[0].status)
            self.assertIn("reserved variable PATH", result.steps[0].error or "")
            self.assertFalse(sentinel.exists())

    def test_tokenless_detached_child_in_worktree_fails_step(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            sentinel = root / "tokenless-survived"
            child_code = (
                "import pathlib,time;"
                "time.sleep(3);"
                f"pathlib.Path({str(sentinel)!r}).write_text('survived')"
            )
            parent_code = (
                "import subprocess,sys;"
                f"subprocess.Popen([{sys.executable!r}, '-c', {child_code!r}], "
                "start_new_session=True, env={})"
            )
            lane = arm.LaneSpec(
                lane_id="tests",
                title="Tokenless child",
                objective="Contain descendants that drop the audit token.",
                required=True,
                timeout_seconds=8,
                exclusive_resources=(),
                steps=(
                    arm.StepSpec(
                        "spawn",
                        (sys.executable, "-c", parent_code),
                        3,
                        {},
                        ".",
                    ),
                ),
            )
            result = arm.run_lane(
                lane,
                root,
                {"version": "0.2.0", "sha": "a" * 40, "workspace": str(root)},
                root / "output",
            )
            time.sleep(3.2)
            self.assertEqual("failed", result.status)
            self.assertEqual("failed", result.steps[0].status)
            self.assertIn("descendant processes", result.steps[0].error or "")
            self.assertFalse(sentinel.exists())


class RepositoryStateTests(unittest.TestCase):
    @staticmethod
    def initialize_repo(
        root: Path, version: str, published: str, changelog: str
    ) -> str:
        (root / "ci").mkdir()
        (root / "VERSION").write_text(version + "\n", encoding="utf-8")
        (root / "ci" / "published-version").write_text(
            published + "\n", encoding="utf-8"
        )
        (root / "CHANGELOG.md").write_text(changelog, encoding="utf-8")
        subprocess.run(
            ["git", "init", "-q", "-b", "main"],
            cwd=root,
            check=True,
            stdout=subprocess.DEVNULL,
        )
        subprocess.run(
            ["git", "config", "user.email", "test@example.com"], cwd=root, check=True
        )
        subprocess.run(["git", "config", "user.name", "Test"], cwd=root, check=True)
        subprocess.run(["git", "add", "."], cwd=root, check=True)
        subprocess.run(
            ["git", "commit", "-m", "fixture"],
            cwd=root,
            check=True,
            stdout=subprocess.DEVNULL,
        )
        return subprocess.check_output(
            ["git", "rev-parse", "HEAD"], cwd=root, text=True
        ).strip()

    def test_unreleased_content_requires_version_advance(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            sha = self.initialize_repo(
                root,
                "0.1.2",
                "0.1.2",
                "# Changelog\n\n## [Unreleased]\n\n* New release work.\n\n## [0.1.2]\n",
            )
            findings = arm.repository_findings(root, "0.1.2", sha)

        self.assertIn(
            "candidate-version-not-advanced",
            {finding["id"] for finding in findings},
        )

    def test_downgrade_is_blocked_even_with_empty_unreleased(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            sha = self.initialize_repo(
                root,
                "0.9.0",
                "1.0.0",
                "# Changelog\n\n## [Unreleased]\n\n## [0.9.0] - 2026-09-05\n",
            )
            findings = arm.repository_findings(root, "0.9.0", sha)
        self.assertIn(
            "candidate-version-downgrade",
            {finding["id"] for finding in findings},
        )

    def test_candidate_requires_dated_changelog_section(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            sha = self.initialize_repo(
                root,
                "1.1.0",
                "1.0.0",
                "# Changelog\n\n## [Unreleased]\n\n## [1.0.0] - 2026-09-01\n",
            )
            findings = arm.repository_findings(root, "1.1.0", sha)
        self.assertIn(
            "candidate-changelog-section-missing",
            {finding["id"] for finding in findings},
        )

    def test_semver_rejects_leading_zeroes(self) -> None:
        self.assertIsNone(arm.SEMVER_RE.fullmatch("01.2.3"))
        self.assertIsNone(arm.SEMVER_RE.fullmatch("1.2.3-01"))
        self.assertIsNotNone(arm.SEMVER_RE.fullmatch("1.2.3-rc.1+build.5"))

    def test_any_blocker_prevents_lane_execution(self) -> None:
        self.assertFalse(
            arm.should_execute_lanes(
                [{"id": "uncut-unreleased-changelog", "severity": "blocker"}]
            )
        )
        self.assertFalse(
            arm.should_execute_lanes(
                [{"id": "candidate-version-downgrade", "severity": "blocker"}]
            )
        )
        self.assertTrue(
            arm.should_execute_lanes(
                [{"id": "audited-commit", "severity": "info"}]
            )
        )


class EvidenceParserTests(unittest.TestCase):
    def test_discovery_count_requires_expected_test_prefix(self) -> None:
        output = """
The following Tests are available:
    Ashlar.Tests.CLI.One
    Ashlar.Tests.CLI.Two(value: 1)
    Other.Tests.Three
"""
        self.assertEqual(
            ["Ashlar.Tests.CLI.One", "Ashlar.Tests.CLI.Two(value: 1)"],
            counted.discovered_tests(output, "Ashlar.Tests.CLI."),
        )

    def test_duplicate_discovery_lines_do_not_inflate_unique_floor(self) -> None:
        output = """
The following Tests are available:
    Ashlar.Tests.CLI.One
    Ashlar.Tests.CLI.One
    Ashlar.Tests.CLI.One
"""
        discovered = counted.discovered_tests(output, "Ashlar.Tests.CLI.")
        self.assertEqual(3, len(discovered))
        self.assertEqual(1, len(set(discovered)))

    def test_trx_execution_count_is_summed(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "result.trx"
            path.write_text(
                """<?xml version="1.0"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Results>
    <UnitTestResult testName="Ashlar.Tests.One" outcome="Passed" />
    <UnitTestResult testName="Ashlar.Tests.Two" outcome="Passed" />
  </Results>
  <ResultSummary outcome="Completed">
    <Counters total="3" executed="2" passed="2" failed="0" />
  </ResultSummary>
</TestRun>
""",
                encoding="utf-8",
            )
            self.assertEqual(2, counted.executed_count(Path(temp)))

    def test_unrelated_test_identities_cannot_satisfy_discovery(self) -> None:
        problems = counted.identity_problems(
            ["Ashlar.Tests.Expected.One", "Ashlar.Tests.Expected.Two"],
            collections.Counter(
                ["Ashlar.Tests.Unrelated.One", "Ashlar.Tests.Unrelated.Two"]
            ),
            collections.Counter(
                ["Ashlar.Tests.Unrelated.One", "Ashlar.Tests.Unrelated.Two"]
            ),
        )
        self.assertTrue(any("did not execute" in problem for problem in problems))

    def test_theory_rows_satisfy_listed_method_identity(self) -> None:
        problems = counted.identity_problems(
            ["Ashlar.Tests.Hosting.KernelPhaseResolutionTests.Profile_ResolvesExpectedKernelServices"],
            collections.Counter(
                [
                    "Ashlar.Tests.Hosting.KernelPhaseResolutionTests.Profile_ResolvesExpectedKernelServices(profile: Full)",
                    "Ashlar.Tests.Hosting.KernelPhaseResolutionTests.Profile_ResolvesExpectedKernelServices(profile: Edge)",
                ]
            ),
            collections.Counter(
                [
                    "Ashlar.Tests.Hosting.KernelPhaseResolutionTests.Profile_ResolvesExpectedKernelServices(profile: Full)",
                    "Ashlar.Tests.Hosting.KernelPhaseResolutionTests.Profile_ResolvesExpectedKernelServices(profile: Edge)",
                ]
            ),
        )
        self.assertEqual([], problems)

    def test_prefix_without_theory_paren_does_not_satisfy_discovery(self) -> None:
        problems = counted.identity_problems(
            ["Ashlar.Tests.Foo"],
            collections.Counter(["Ashlar.Tests.FooBar"]),
            collections.Counter(["Ashlar.Tests.FooBar"]),
        )
        self.assertTrue(any("did not execute" in problem for problem in problems))

    def test_vulnerability_records_find_nested_packages(self) -> None:
        report = {
            "projects": [
                {
                    "frameworks": [
                        {
                            "topLevelPackages": [
                                {"id": "safe", "vulnerabilities": []},
                                {
                                    "id": "unsafe",
                                    "vulnerabilities": [{"severity": "High"}],
                                },
                            ]
                        }
                    ]
                }
            ]
        }
        records = vulnerabilities.vulnerability_records(report)
        self.assertEqual(["unsafe"], [record["id"] for record in records])

    def test_vulnerability_report_rejects_empty_and_problem_evidence(self) -> None:
        expected = {str(Path("/repo/project.csproj").resolve())}
        self.assertTrue(vulnerabilities.validate_report({}, expected))
        report = {
            "version": 1,
            "parameters": "--vulnerable --include-transitive",
            "sources": ["https://api.nuget.org/v3/index.json"],
            "projects": [],
            "problems": ["NU1900: advisory source unavailable"],
        }
        problems = vulnerabilities.validate_report(report, expected)
        self.assertTrue(any("NuGet reported problems" in problem for problem in problems))
        self.assertTrue(any("project identities differ" in problem for problem in problems))

    def test_vulnerability_report_rejects_duplicate_project_identities(self) -> None:
        project = str(Path("/repo/project.csproj").resolve())
        report = {
            "version": 1,
            "parameters": "--vulnerable --include-transitive",
            "sources": ["https://api.nuget.org/v3/index.json"],
            "projects": [{"path": project}, {"path": project}],
        }
        problems = vulnerabilities.validate_report(report, {project})
        self.assertTrue(any("duplicate project paths" in problem for problem in problems))

    def test_vulnerability_report_rejects_untrusted_advisory_source(self) -> None:
        project = str(Path("/repo/project.csproj").resolve())
        report = {
            "version": 1,
            "parameters": "--vulnerable --include-transitive",
            "sources": ["https://example.invalid/advisories"],
            "projects": [{"path": project}],
        }
        problems = vulnerabilities.validate_report(report, {project})
        self.assertTrue(
            any("trusted nuget.org advisory source" in problem for problem in problems)
        )


class ReleaseScriptSafetyTests(unittest.TestCase):
    def test_preflight_refuses_version_mismatch_before_pack(self) -> None:
        repo = SCRIPT.parents[1]
        run = subprocess.run(
            ["bash", "scripts/release-preflight-local.sh", "9.9.9"],
            cwd=repo,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
        )
        self.assertNotEqual(0, run.returncode)
        self.assertIn("does not match VERSION file", run.stdout)
        self.assertNotIn("Pack graph vs MSBuild", run.stdout)

    def test_prod_dry_run_refuses_dead_daemon(self) -> None:
        repo = SCRIPT.parents[1]
        with tempfile.TemporaryDirectory() as temp:
            fake_bin = Path(temp)
            docker_log = fake_bin / "docker.log"
            docker = fake_bin / "docker"
            docker.write_text(
                f"#!/usr/bin/env bash\nprintf '%s\\n' \"$*\" >> {str(docker_log)!r}\n"
                "if [ \"$1\" = info ]; then exit 1; fi\nexit 0\n",
                encoding="utf-8",
            )
            docker.chmod(0o755)
            environment = os.environ.copy()
            environment["PATH"] = f"{fake_bin}{os.pathsep}{environment['PATH']}"
            run = subprocess.run(
                ["bash", "scripts/prod-dry-run.sh", "--portal", "--no-build"],
                cwd=repo,
                env=environment,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
            )
            self.assertEqual(2, run.returncode)
            self.assertIn("working Docker daemon", run.stdout)
            self.assertNotIn("compose ", docker_log.read_text(encoding="utf-8"))

    def test_prod_dry_run_cleans_stack_on_failure(self) -> None:
        repo = SCRIPT.parents[1]
        with tempfile.TemporaryDirectory() as temp:
            fake_bin = Path(temp)
            docker_log = fake_bin / "docker.log"
            docker = fake_bin / "docker"
            docker.write_text(
                f"#!/usr/bin/env bash\nprintf '%s\\n' \"$*\" >> {str(docker_log)!r}\nexit 0\n",
                encoding="utf-8",
            )
            docker.chmod(0o755)
            curl = fake_bin / "curl"
            curl.write_text("#!/usr/bin/env bash\nexit 22\n", encoding="utf-8")
            curl.chmod(0o755)
            environment = os.environ.copy()
            environment["PATH"] = f"{fake_bin}{os.pathsep}{environment['PATH']}"
            environment["ASHLAR_RELEASE_AUDIT"] = "1"
            environment["COMPOSE_PROJECT_NAME"] = "ashlar-release-manager-test"
            run = subprocess.run(
                ["bash", "scripts/prod-dry-run.sh", "--portal", "--no-build"],
                cwd=repo,
                env=environment,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
            )

            self.assertNotEqual(0, run.returncode)
            self.assertIn(
                "compose -f deploy/compose/docker-compose.portal.yml down --remove-orphans --volumes",
                docker_log.read_text(encoding="utf-8"),
            )

    def test_dependency_boundary_skips_generated_ashlar_trees(self) -> None:
        repo = SCRIPT.parents[1]
        module = load_script(
            "verify_open_commercial_dependency_boundary",
            repo / "scripts" / "verify-open-commercial-dependency-boundary.py",
        )
        discovered = {
            module.rel_posix(path, repo) for path in module.discover_csprojs(repo)
        }
        self.assertFalse(
            any(rel.startswith(".ashlar/") for rel in discovered),
            discovered,
        )

    def test_container_image_publish_is_dispatch_only(self) -> None:
        repo = SCRIPT.parents[1]
        text = (
            repo / ".github" / "workflows" / "container-image-publish.yml"
        ).read_text(encoding="utf-8")
        self.assertIn("workflow_dispatch:", text)
        self.assertNotIn("\n  push:", text)

    def test_versioned_publish_workflows_require_ready(self) -> None:
        repo = SCRIPT.parents[1]
        release = (repo / ".github" / "workflows" / "release.yml").read_text(
            encoding="utf-8"
        )
        nuget = (repo / ".github" / "workflows" / "release-nuget.yml").read_text(
            encoding="utf-8"
        )
        self.assertIn("needs: [validate, release-ready]", release)
        self.assertIn("require-release-manager-ready.sh", release)
        self.assertIn("needs: [release-ready]", nuget)
        self.assertIn("require-release-manager-ready.sh", nuget)

    def test_require_ready_blocks_unpublished_unreleased_tree(self) -> None:
        repo = SCRIPT.parents[1]
        with tempfile.TemporaryDirectory() as temp:
            run = subprocess.run(
                [
                    "bash",
                    "scripts/require-release-manager-ready.sh",
                    "",
                    str(Path(temp) / "report"),
                ],
                cwd=repo,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
            )
        self.assertNotEqual(0, run.returncode)
        self.assertIn("Publish is blocked until the autonomous release manager verdict is READY", run.stdout)

    def test_publish_workflows_pin_actions_and_sbom_install(self) -> None:
        repo = SCRIPT.parents[1]
        publish_files = (
            repo / ".github" / "workflows" / "reusable-release-nuget.yml",
            repo / ".github" / "workflows" / "reusable-container-publish.yml",
            repo / ".github" / "workflows" / "reusable-verify-nuget-consumer.yml",
            repo / ".github" / "workflows" / "release.yml",
            repo / ".github" / "workflows" / "release-nuget.yml",
        )
        floating = []
        unpinned_sbom = []
        for path in publish_files:
            text = path.read_text(encoding="utf-8")
            for line in text.splitlines():
                stripped = line.strip()
                if stripped.startswith("uses:") and "@v" in stripped and "#" not in stripped:
                    floating.append(f"{path.name}: {stripped}")
                if "raw.githubusercontent.com/anchore" in stripped and "install.sh" in stripped:
                    unpinned_sbom.append(f"{path.name}: {stripped}")
        self.assertEqual([], floating)
        self.assertEqual([], unpinned_sbom)
        installer = (
            repo / "scripts" / "install-anchore-sbom-tools.sh"
        ).read_text(encoding="utf-8")
        self.assertIn('SYFT_VERSION="1.51.0"', installer)
        self.assertIn("syft_${SYFT_VERSION}_linux_amd64.tar.gz", installer)
        self.assertIn(
            "2a2e837a2c8d59ec9af5472ee22d3b04ee463c4e44476ecf993fd1e5ab6ebc7f",
            installer,
        )

    def test_sbom_installer_rejects_checksum_mismatch(self) -> None:
        repo = SCRIPT.parents[1]
        with tempfile.TemporaryDirectory() as temp:
            fake_bin = Path(temp) / "bin"
            dest = Path(temp) / "tools"
            fake_bin.mkdir()
            curl = fake_bin / "curl"
            curl.write_text(
                "#!/usr/bin/env bash\n"
                "out=''\n"
                "while [ $# -gt 0 ]; do\n"
                "  if [ \"$1\" = \"-o\" ]; then out=\"$2\"; shift 2; continue; fi\n"
                "  shift\n"
                "done\n"
                "printf 'not-syft' > \"$out\"\n",
                encoding="utf-8",
            )
            curl.chmod(0o755)
            environment = os.environ.copy()
            environment["PATH"] = f"{fake_bin}{os.pathsep}{environment['PATH']}"
            run = subprocess.run(
                ["bash", "scripts/install-anchore-sbom-tools.sh", str(dest)],
                cwd=repo,
                env=environment,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
            )
            self.assertNotEqual(0, run.returncode)
            self.assertIn("checksum mismatch", run.stdout)


if __name__ == "__main__":
    unittest.main()
