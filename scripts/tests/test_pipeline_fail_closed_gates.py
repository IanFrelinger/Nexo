#!/usr/bin/env python3
"""Guard Production Readiness Gate v1 fail-closed helpers and local mirrors."""
from __future__ import annotations

import json
import os
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


if __name__ == "__main__":
    unittest.main()
