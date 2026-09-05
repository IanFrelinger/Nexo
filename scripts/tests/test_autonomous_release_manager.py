from __future__ import annotations

import concurrent.futures
import importlib.util
import json
import subprocess
import sys
import tempfile
import threading
import time
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "autonomous-release-manager.py"
SPEC = importlib.util.spec_from_file_location("autonomous_release_manager", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
arm = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = arm
SPEC.loader.exec_module(arm)


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

    def test_parallelism_cannot_exceed_lane_count(self) -> None:
        document = plan_document()
        document["maxParallel"] = 7
        with tempfile.TemporaryDirectory() as temp:
            with self.assertRaisesRegex(arm.PlanError, "cannot exceed 6"):
                arm.load_plan(self.write_plan(Path(temp), document))


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


class RepositoryStateTests(unittest.TestCase):
    def test_unreleased_content_requires_version_advance(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "ci").mkdir()
            (root / "VERSION").write_text("0.1.2\n", encoding="utf-8")
            (root / "ci" / "published-version").write_text("0.1.2\n", encoding="utf-8")
            (root / "CHANGELOG.md").write_text(
                "# Changelog\n\n## [Unreleased]\n\n- New release work.\n\n## [0.1.2]\n",
                encoding="utf-8",
            )
            subprocess.run(
                ["git", "init", "-q", "-b", "main"],
                cwd=root,
                check=True,
                stdout=subprocess.DEVNULL,
            )
            subprocess.run(
                ["git", "config", "user.email", "test@example.com"], cwd=root, check=True
            )
            subprocess.run(
                ["git", "config", "user.name", "Test"], cwd=root, check=True
            )
            subprocess.run(["git", "add", "."], cwd=root, check=True)
            subprocess.run(
                ["git", "commit", "-m", "fixture"],
                cwd=root,
                check=True,
                stdout=subprocess.DEVNULL,
            )
            sha = subprocess.check_output(
                ["git", "rev-parse", "HEAD"], cwd=root, text=True
            ).strip()

            findings = arm.repository_findings(root, "0.1.2", sha)

        self.assertIn(
            "candidate-version-not-advanced",
            {finding["id"] for finding in findings},
        )


if __name__ == "__main__":
    unittest.main()
