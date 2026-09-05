#!/usr/bin/env python3
"""
Autonomous, fail-closed release-readiness coordinator.

The coordinator runs the six canonical audit lanes in isolated git worktrees,
records every command and log digest, and emits one machine-readable verdict.
It never publishes, tags, pushes, or changes production state.
"""

from __future__ import annotations

import argparse
import concurrent.futures
import dataclasses
import datetime as dt
import hashlib
import json
import os
import re
import shutil
import signal
import subprocess
import sys
import tempfile
import threading
import time
from pathlib import Path
from typing import Any


SCHEMA_VERSION = 1
CANONICAL_LANES = {
    "code",
    "tests",
    "security",
    "packaging",
    "documentation",
    "operations",
}
SPECIALIST_AGENTS = (
    "code-auditor",
    "ci-auditor",
    "security-auditor",
    "packaging-auditor",
    "documentation-auditor",
    "operations-auditor",
)
SEMVER_RE = re.compile(
    r"^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?"
    r"(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$"
)
FORBIDDEN_COMMAND_FRAGMENTS = (
    "git push",
    "git tag",
    "gh release create",
    "gh release upload",
    "gh workflow run release",
    "dotnet nuget push",
    "docker push",
    "make release-dispatch",
    "make release-staging",
    "release-staging.sh",
    "npm publish",
    "cargo publish",
    "twine upload",
)
SHELL_EXECUTABLES = {
    "bash",
    "cmd",
    "cmd.exe",
    "pwsh",
    "powershell",
    "powershell.exe",
    "sh",
    "zsh",
}
SHELL_CODE_FLAGS = {"-c", "/c", "-command", "-encodedcommand"}


class PlanError(ValueError):
    """Raised when an audit plan could weaken the release gate."""


@dataclasses.dataclass(frozen=True)
class StepSpec:
    name: str
    command: tuple[str, ...]
    timeout_seconds: int | None
    environment: dict[str, str]
    working_directory: str


@dataclasses.dataclass(frozen=True)
class LaneSpec:
    lane_id: str
    title: str
    objective: str
    required: bool
    timeout_seconds: int
    exclusive_resources: tuple[str, ...]
    steps: tuple[StepSpec, ...]


@dataclasses.dataclass(frozen=True)
class AuditPlan:
    max_parallel: int
    lanes: tuple[LaneSpec, ...]


@dataclasses.dataclass(frozen=True)
class StepResult:
    name: str
    command: tuple[str, ...]
    status: str
    exit_code: int | None
    duration_seconds: float
    timeout_seconds: int
    error: str | None = None


@dataclasses.dataclass(frozen=True)
class LaneResult:
    lane_id: str
    title: str
    objective: str
    required: bool
    status: str
    duration_seconds: float
    log_path: str
    log_sha256: str
    steps: tuple[StepResult, ...]


def _expect_object(value: Any, name: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise PlanError(f"{name} must be an object.")
    return value


def _expect_string(value: Any, name: str) -> str:
    if not isinstance(value, str) or not value.strip():
        raise PlanError(f"{name} must be a non-blank string.")
    return value.strip()


def _expect_positive_int(value: Any, name: str) -> int:
    if isinstance(value, bool) or not isinstance(value, int) or value < 1:
        raise PlanError(f"{name} must be a positive integer.")
    return value


def _validate_relative_directory(value: str, name: str) -> str:
    path = Path(value)
    if path.is_absolute() or ".." in path.parts:
        raise PlanError(f"{name} must stay inside the isolated worktree.")
    return value


def _validate_command(command: tuple[str, ...], name: str) -> None:
    joined = " ".join(command).lower()
    if any(fragment in joined for fragment in FORBIDDEN_COMMAND_FRAGMENTS):
        raise PlanError(f"{name} contains a publishing or repository-write command.")

    executable = Path(command[0]).name.lower()
    if executable in SHELL_EXECUTABLES and len(command) > 1:
        if command[1].lower() in SHELL_CODE_FLAGS:
            raise PlanError(
                f"{name} uses inline shell code. Use an audited repository script instead."
            )


def load_plan(path: Path) -> AuditPlan:
    try:
        root = _expect_object(json.loads(path.read_text(encoding="utf-8")), "plan")
    except (OSError, json.JSONDecodeError) as exc:
        raise PlanError(f"Could not read audit plan {path}: {exc}") from exc

    if root.get("schemaVersion") != SCHEMA_VERSION:
        raise PlanError(
            f"plan.schemaVersion must be {SCHEMA_VERSION}; got {root.get('schemaVersion')!r}."
        )

    max_parallel = _expect_positive_int(root.get("maxParallel"), "plan.maxParallel")
    if max_parallel > len(CANONICAL_LANES):
        raise PlanError(
            f"plan.maxParallel cannot exceed {len(CANONICAL_LANES)} audit lanes."
        )
    lanes_raw = root.get("lanes")
    if not isinstance(lanes_raw, list) or not lanes_raw:
        raise PlanError("plan.lanes must be a non-empty array.")

    lanes: list[LaneSpec] = []
    seen_lanes: set[str] = set()
    for lane_index, lane_value in enumerate(lanes_raw):
        lane_raw = _expect_object(lane_value, f"plan.lanes[{lane_index}]")
        lane_id = _expect_string(lane_raw.get("id"), f"plan.lanes[{lane_index}].id")
        if lane_id in seen_lanes:
            raise PlanError(f"Duplicate lane id: {lane_id}.")
        seen_lanes.add(lane_id)

        required = lane_raw.get("required")
        if not isinstance(required, bool):
            raise PlanError(f"lane {lane_id}.required must be true or false.")

        steps_raw = lane_raw.get("steps")
        if not isinstance(steps_raw, list) or not steps_raw:
            raise PlanError(f"lane {lane_id} must define at least one step.")

        lane_timeout = _expect_positive_int(
            lane_raw.get("timeoutSeconds"), f"lane {lane_id}.timeoutSeconds"
        )
        resources_raw = lane_raw.get("exclusiveResources", [])
        if not isinstance(resources_raw, list):
            raise PlanError(f"lane {lane_id}.exclusiveResources must be an array.")
        exclusive_resources = tuple(
            sorted(
                {
                    _expect_string(resource, f"lane {lane_id} exclusive resource")
                    for resource in resources_raw
                }
            )
        )
        steps: list[StepSpec] = []
        seen_steps: set[str] = set()
        for step_index, step_value in enumerate(steps_raw):
            step_raw = _expect_object(step_value, f"lane {lane_id}.steps[{step_index}]")
            step_name = _expect_string(
                step_raw.get("name"), f"lane {lane_id}.steps[{step_index}].name"
            )
            if step_name in seen_steps:
                raise PlanError(f"lane {lane_id} has duplicate step name {step_name}.")
            seen_steps.add(step_name)

            command_raw = step_raw.get("command")
            if not isinstance(command_raw, list) or not command_raw:
                raise PlanError(f"lane {lane_id} step {step_name} needs a command array.")
            command = tuple(
                _expect_string(part, f"lane {lane_id} step {step_name} command")
                for part in command_raw
            )
            _validate_command(command, f"lane {lane_id} step {step_name}")

            timeout_value = step_raw.get("timeoutSeconds")
            timeout = (
                _expect_positive_int(
                    timeout_value, f"lane {lane_id} step {step_name}.timeoutSeconds"
                )
                if timeout_value is not None
                else None
            )
            environment_raw = step_raw.get("environment", {})
            environment_obj = _expect_object(
                environment_raw, f"lane {lane_id} step {step_name}.environment"
            )
            environment = {
                _expect_string(key, f"lane {lane_id} step {step_name} environment key"):
                _expect_string(value, f"lane {lane_id} step {step_name} environment value")
                for key, value in environment_obj.items()
            }
            working_directory = _validate_relative_directory(
                str(step_raw.get("workingDirectory", ".")),
                f"lane {lane_id} step {step_name}.workingDirectory",
            )
            steps.append(
                StepSpec(
                    name=step_name,
                    command=command,
                    timeout_seconds=timeout,
                    environment=environment,
                    working_directory=working_directory,
                )
            )

        lanes.append(
            LaneSpec(
                lane_id=lane_id,
                title=_expect_string(lane_raw.get("title"), f"lane {lane_id}.title"),
                objective=_expect_string(
                    lane_raw.get("objective"), f"lane {lane_id}.objective"
                ),
                required=required,
                timeout_seconds=lane_timeout,
                exclusive_resources=exclusive_resources,
                steps=tuple(steps),
            )
        )

    missing = CANONICAL_LANES - seen_lanes
    extra = seen_lanes - CANONICAL_LANES
    if missing or extra:
        raise PlanError(
            "The canonical audit lanes are immutable. "
            f"Missing={sorted(missing)} extra={sorted(extra)}."
        )
    non_required = sorted(lane.lane_id for lane in lanes if not lane.required)
    if non_required:
        raise PlanError(
            "All canonical lanes must be release-blocking; "
            f"non-required={non_required}."
        )

    return AuditPlan(max_parallel=max_parallel, lanes=tuple(lanes))


def _frontmatter(path: Path) -> tuple[dict[str, str], str]:
    try:
        text = path.read_text(encoding="utf-8")
    except OSError as exc:
        raise PlanError(f"Required Cursor asset is missing: {path}") from exc
    if not text.startswith("---\n") or "\n---\n" not in text[4:]:
        raise PlanError(f"Cursor asset lacks YAML frontmatter: {path}")
    raw, body = text[4:].split("\n---\n", 1)
    metadata: dict[str, str] = {}
    for line in raw.splitlines():
        if ":" not in line:
            raise PlanError(f"Malformed Cursor frontmatter in {path}: {line!r}")
        key, value = line.split(":", 1)
        metadata[key.strip()] = value.strip()
    return metadata, body


def validate_cursor_assets(repo_root: Path) -> None:
    agents_root = repo_root / ".cursor" / "agents"
    for agent_name in ("release-manager", *SPECIALIST_AGENTS):
        path = agents_root / f"{agent_name}.md"
        metadata, _ = _frontmatter(path)
        if metadata.get("name") != agent_name:
            raise PlanError(f"{path} name must be {agent_name!r}.")
        expected_readonly = "false" if agent_name == "release-manager" else "true"
        if metadata.get("readonly") != expected_readonly:
            raise PlanError(
                f"{path} readonly must be {expected_readonly} for its release role."
            )
        if agent_name != "release-manager" and metadata.get("is_background") != "true":
            raise PlanError(f"{path} must set is_background: true.")

    manager_metadata, manager_body = _frontmatter(
        agents_root / "release-manager.md"
    )
    del manager_metadata
    for specialist in SPECIALIST_AGENTS:
        if specialist not in manager_body:
            raise PlanError(
                f"release-manager.md does not delegate to {specialist}."
            )

    skill_path = repo_root / ".cursor" / "skills" / "release-manager" / "SKILL.md"
    skill_metadata, skill_body = _frontmatter(skill_path)
    if skill_metadata.get("name") != "release-manager":
        raise PlanError(f"{skill_path} name must be 'release-manager'.")
    for specialist in SPECIALIST_AGENTS:
        if specialist not in skill_body:
            raise PlanError(f"{skill_path} does not require {specialist}.")

    rule_path = (
        repo_root / ".cursor" / "rules" / "release-publishing-safety.mdc"
    )
    rule_metadata, rule_body = _frontmatter(rule_path)
    if rule_metadata.get("alwaysApply") != "true":
        raise PlanError(f"{rule_path} must set alwaysApply: true.")
    if "explicitly authorizes" not in rule_body:
        raise PlanError(f"{rule_path} must require explicit publishing authorization.")


def _render(value: str, context: dict[str, str]) -> str:
    rendered = value
    for key, replacement in context.items():
        rendered = rendered.replace("{" + key + "}", replacement)
    if re.search(r"\{[A-Za-z][A-Za-z0-9_]*\}", rendered):
        raise PlanError(f"Unknown template variable in {value!r}.")
    return rendered


def _terminate_process_tree(process: subprocess.Popen[bytes]) -> None:
    if process.poll() is not None:
        return
    if os.name == "nt":
        subprocess.run(
            ["taskkill", "/PID", str(process.pid), "/T", "/F"],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            check=False,
        )
        return

    try:
        os.killpg(process.pid, signal.SIGTERM)
        process.wait(timeout=5)
    except (ProcessLookupError, subprocess.TimeoutExpired):
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass


def run_step(
    step: StepSpec,
    worktree: Path,
    context: dict[str, str],
    log_handle: Any,
    timeout_seconds: int,
) -> StepResult:
    command = tuple(_render(part, context) for part in step.command)
    working_directory = (worktree / step.working_directory).resolve()
    try:
        working_directory.relative_to(worktree.resolve())
    except ValueError:
        return StepResult(
            name=step.name,
            command=command,
            status="error",
            exit_code=None,
            duration_seconds=0,
            timeout_seconds=timeout_seconds,
            error="Working directory escaped the isolated worktree.",
        )

    environment = os.environ.copy()
    environment.update(
        {_render(key, context): _render(value, context) for key, value in step.environment.items()}
    )
    environment["ASHLAR_RELEASE_AUDIT"] = "1"

    log_handle.write(
        (
            f"\n=== step: {step.name} ===\n"
            f"cwd: {working_directory}\n"
            f"command: {json.dumps(command)}\n"
            f"timeout_seconds: {timeout_seconds}\n"
        ).encode()
    )
    log_handle.flush()

    started = time.monotonic()
    try:
        process = subprocess.Popen(
            command,
            cwd=working_directory,
            env=environment,
            stdout=log_handle,
            stderr=subprocess.STDOUT,
            start_new_session=os.name != "nt",
            creationflags=(
                subprocess.CREATE_NEW_PROCESS_GROUP if os.name == "nt" else 0
            ),
        )
    except OSError as exc:
        return StepResult(
            name=step.name,
            command=command,
            status="error",
            exit_code=None,
            duration_seconds=round(time.monotonic() - started, 3),
            timeout_seconds=timeout_seconds,
            error=str(exc),
        )

    try:
        exit_code = process.wait(timeout=timeout_seconds)
        status = "passed" if exit_code == 0 else "failed"
        error = None
    except subprocess.TimeoutExpired:
        _terminate_process_tree(process)
        exit_code = process.poll()
        status = "timeout"
        error = f"Exceeded {timeout_seconds} seconds."

    duration = round(time.monotonic() - started, 3)
    log_handle.write(
        (
            f"\nstep_status: {status}\n"
            f"step_exit_code: {exit_code}\n"
            f"step_duration_seconds: {duration}\n"
        ).encode()
    )
    log_handle.flush()
    return StepResult(
        name=step.name,
        command=command,
        status=status,
        exit_code=exit_code,
        duration_seconds=duration,
        timeout_seconds=timeout_seconds,
        error=error,
    )


def run_lane(
    lane: LaneSpec,
    worktree: Path,
    context: dict[str, str],
    output_directory: Path,
) -> LaneResult:
    lane_started = time.monotonic()
    log_path = output_directory / "logs" / f"{lane.lane_id}.log"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    deadline = lane_started + lane.timeout_seconds
    step_results: list[StepResult] = []

    with log_path.open("wb") as log_handle:
        log_handle.write(
            (
                f"lane: {lane.lane_id}\n"
                f"title: {lane.title}\n"
                f"objective: {lane.objective}\n"
                f"head_sha: {context['sha']}\n"
                f"version: {context['version']}\n"
            ).encode()
        )
        for step in lane.steps:
            remaining = int(deadline - time.monotonic())
            if remaining < 1:
                step_results.append(
                    StepResult(
                        name=step.name,
                        command=step.command,
                        status="timeout",
                        exit_code=None,
                        duration_seconds=0,
                        timeout_seconds=0,
                        error="Lane timeout exhausted before this step started.",
                    )
                )
                continue
            timeout = min(step.timeout_seconds or remaining, remaining)
            step_results.append(
                run_step(step, worktree, context, log_handle, timeout)
            )

    status = (
        "passed"
        if step_results and all(step.status == "passed" for step in step_results)
        else "failed"
    )
    digest = hashlib.sha256(log_path.read_bytes()).hexdigest()
    return LaneResult(
        lane_id=lane.lane_id,
        title=lane.title,
        objective=lane.objective,
        required=lane.required,
        status=status,
        duration_seconds=round(time.monotonic() - lane_started, 3),
        log_path=str(log_path.relative_to(output_directory)),
        log_sha256=digest,
        steps=tuple(step_results),
    )


def run_lane_with_resources(
    lane: LaneSpec,
    worktree: Path,
    context: dict[str, str],
    output_directory: Path,
    resource_locks: dict[str, threading.Lock],
) -> LaneResult:
    acquired: list[threading.Lock] = []
    try:
        for resource in lane.exclusive_resources:
            lock = resource_locks[resource]
            lock.acquire()
            acquired.append(lock)
        return run_lane(lane, worktree, context, output_directory)
    finally:
        for lock in reversed(acquired):
            lock.release()


def repository_findings(repo_root: Path, version: str, sha: str) -> list[dict[str, str]]:
    findings: list[dict[str, str]] = []
    canonical = (repo_root / "VERSION").read_text(encoding="utf-8").strip()
    if version != canonical:
        findings.append(
            {
                "id": "version-mismatch",
                "severity": "blocker",
                "message": f"Requested version {version} does not match VERSION ({canonical}).",
            }
        )
    if not SEMVER_RE.fullmatch(canonical):
        findings.append(
            {
                "id": "invalid-version",
                "severity": "blocker",
                "message": f"VERSION is not SemVer: {canonical!r}.",
            }
        )

    published_path = repo_root / "ci" / "published-version"
    if published_path.exists():
        published = published_path.read_text(encoding="utf-8").strip()
        changelog = (repo_root / "CHANGELOG.md").read_text(encoding="utf-8")
        unreleased = changelog.partition("## [Unreleased]")[2].partition("\n## [")[0]
        meaningful = any(
            line.startswith("- ") for line in unreleased.splitlines()
        )
        if canonical == published and meaningful:
            findings.append(
                {
                    "id": "candidate-version-not-advanced",
                    "severity": "blocker",
                    "message": (
                        f"VERSION still equals published pin {published} while "
                        "CHANGELOG [Unreleased] contains release content."
                    ),
                }
            )

    status = subprocess.run(
        ["git", "status", "--porcelain", "--untracked-files=no"],
        cwd=repo_root,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if status.returncode != 0 or status.stdout.strip():
        findings.append(
            {
                "id": "dirty-release-sha",
                "severity": "blocker",
                "message": "Release audit must run from a clean tracked worktree.",
            }
        )

    if subprocess.run(
        ["git", "rev-parse", "--verify", "--quiet", f"refs/tags/v{canonical}"],
        cwd=repo_root,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        check=False,
    ).returncode == 0 and canonical != (
        published_path.read_text(encoding="utf-8").strip()
        if published_path.exists()
        else ""
    ):
        findings.append(
            {
                "id": "candidate-tag-already-exists",
                "severity": "blocker",
                "message": f"Tag v{canonical} already exists but is not the published pin.",
            }
        )

    findings.append(
        {
            "id": "audited-commit",
            "severity": "info",
            "message": f"Audit target resolved to commit {sha}.",
        }
    )
    return findings


def _git(repo_root: Path, *args: str) -> str:
    result = subprocess.run(
        ["git", *args],
        cwd=repo_root,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if result.returncode != 0:
        raise RuntimeError(
            f"git {' '.join(args)} failed ({result.returncode}): {result.stderr.strip()}"
        )
    return result.stdout.strip()


def prepare_worktrees(
    repo_root: Path,
    lanes: tuple[LaneSpec, ...],
    sha: str,
    temp_root: Path,
    worktrees: dict[str, Path],
) -> dict[str, Path]:
    for lane in lanes:
        path = temp_root / lane.lane_id
        _git(repo_root, "worktree", "add", "--detach", str(path), sha)
        worktrees[lane.lane_id] = path
    return worktrees


def cleanup_worktrees(repo_root: Path, worktrees: dict[str, Path]) -> None:
    for path in worktrees.values():
        subprocess.run(
            ["git", "worktree", "remove", "--force", str(path)],
            cwd=repo_root,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            check=False,
        )
    subprocess.run(
        ["git", "worktree", "prune"],
        cwd=repo_root,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        check=False,
    )


def _result_to_dict(result: LaneResult) -> dict[str, Any]:
    return {
        "id": result.lane_id,
        "title": result.title,
        "objective": result.objective,
        "required": result.required,
        "status": result.status,
        "durationSeconds": result.duration_seconds,
        "logPath": result.log_path,
        "logSha256": result.log_sha256,
        "steps": [
            {
                "name": step.name,
                "command": list(step.command),
                "status": step.status,
                "exitCode": step.exit_code,
                "durationSeconds": step.duration_seconds,
                "timeoutSeconds": step.timeout_seconds,
                "error": step.error,
            }
            for step in result.steps
        ],
    }


def write_reports(
    output_directory: Path,
    run_id: str,
    started_at: str,
    completed_at: str,
    sha: str,
    version: str,
    findings: list[dict[str, str]],
    lane_results: list[LaneResult],
) -> tuple[Path, Path, str]:
    blocked_findings = [
        finding for finding in findings if finding["severity"] == "blocker"
    ]
    failed_lanes = [result for result in lane_results if result.required and result.status != "passed"]
    verdict = "blocked" if blocked_findings or failed_lanes else "ready"

    report = {
        "schemaVersion": SCHEMA_VERSION,
        "runId": run_id,
        "startedAt": started_at,
        "completedAt": completed_at,
        "headSha": sha,
        "version": version,
        "verdict": verdict,
        "summary": {
            "totalLanes": len(lane_results),
            "passedLanes": sum(result.status == "passed" for result in lane_results),
            "failedLanes": sum(result.status != "passed" for result in lane_results),
            "blockingFindings": len(blocked_findings),
        },
        "repositoryFindings": findings,
        "lanes": [_result_to_dict(result) for result in lane_results],
    }
    json_path = output_directory / "report.json"
    json_path.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")

    lines = [
        "# Autonomous release-manager report",
        "",
        f"- Verdict: **{verdict.upper()}**",
        f"- Version: `{version}`",
        f"- Commit: `{sha}`",
        f"- Run: `{run_id}`",
        "",
        "## Repository findings",
        "",
    ]
    for finding in findings:
        lines.append(
            f"- **{finding['severity'].upper()}** `{finding['id']}` — {finding['message']}"
        )
    lines.extend(
        [
            "",
            "## Audit sub-agents",
            "",
            "| Lane | Status | Duration | Evidence |",
            "|------|--------|----------|----------|",
        ]
    )
    for result in lane_results:
        lines.append(
            f"| `{result.lane_id}` | **{result.status.upper()}** | "
            f"{result.duration_seconds:.1f}s | `{result.log_path}` "
            f"(`sha256:{result.log_sha256}`) |"
        )
    lines.extend(["", "## Failed steps", ""])
    failed_steps = [
        (result, step)
        for result in lane_results
        for step in result.steps
        if step.status != "passed"
    ]
    if failed_steps:
        for result, step in failed_steps:
            lines.append(
                f"- `{result.lane_id}/{step.name}` — {step.status}; "
                f"exit={step.exit_code}; {step.error or 'see lane log'}"
            )
    else:
        lines.append("- None.")
    markdown_path = output_directory / "report.md"
    markdown_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    return json_path, markdown_path, verdict


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Run all canonical release audit sub-agents and emit a fail-closed verdict."
    )
    parser.add_argument(
        "--plan",
        default="ci/autonomous-release-manager.json",
        help="Repository-relative audit plan.",
    )
    parser.add_argument(
        "--version",
        help="Candidate SemVer. Must exactly match the root VERSION file.",
    )
    parser.add_argument(
        "--output",
        help="Report directory. Defaults to .ashlar/release-manager/runs/<run-id>.",
    )
    parser.add_argument(
        "--validate-only",
        action="store_true",
        help="Validate the plan and safety policy without running commands.",
    )
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv or sys.argv[1:])
    repo_root = Path(__file__).resolve().parent.parent
    try:
        plan_path = (repo_root / args.plan).resolve()
        plan_path.relative_to(repo_root)
        plan = load_plan(plan_path)
        validate_cursor_assets(repo_root)
    except (PlanError, ValueError) as exc:
        print(f"release-manager: invalid plan: {exc}", file=sys.stderr)
        return 64

    if args.validate_only:
        print(
            "release-manager: plan valid; canonical required lanes="
            + ",".join(sorted(CANONICAL_LANES))
        )
        return 0

    canonical_version = (repo_root / "VERSION").read_text(encoding="utf-8").strip()
    version = (args.version or canonical_version).removeprefix("v")
    sha = _git(repo_root, "rev-parse", "HEAD")
    started = dt.datetime.now(dt.timezone.utc)
    run_id = f"{started.strftime('%Y%m%dT%H%M%SZ')}-{sha[:12]}"
    output_directory = (
        (repo_root / args.output).resolve()
        if args.output
        else repo_root / ".ashlar" / "release-manager" / "runs" / run_id
    )
    output_directory.mkdir(parents=True, exist_ok=True)
    findings = repository_findings(repo_root, version, sha)

    lane_results: list[LaneResult] = []
    worktrees: dict[str, Path] = {}
    try:
        with tempfile.TemporaryDirectory(prefix="ashlar-release-audit-") as temp:
            temp_root = Path(temp)
            try:
                prepare_worktrees(repo_root, plan.lanes, sha, temp_root, worktrees)
                resource_locks = {
                    resource: threading.Lock()
                    for lane in plan.lanes
                    for resource in lane.exclusive_resources
                }
                with concurrent.futures.ThreadPoolExecutor(
                    max_workers=plan.max_parallel
                ) as executor:
                    future_by_lane = {
                        executor.submit(
                            run_lane_with_resources,
                            lane,
                            worktrees[lane.lane_id],
                            {
                                "version": version,
                                "sha": sha,
                                "workspace": str(worktrees[lane.lane_id]),
                            },
                            output_directory,
                            resource_locks,
                        ): lane
                        for lane in plan.lanes
                    }
                    for future in concurrent.futures.as_completed(future_by_lane):
                        lane = future_by_lane[future]
                        try:
                            lane_results.append(future.result())
                        except Exception as exc:  # fail closed; retain other lane evidence
                            log_path = output_directory / "logs" / f"{lane.lane_id}.log"
                            log_path.parent.mkdir(parents=True, exist_ok=True)
                            log_path.write_text(
                                f"lane coordinator error: {type(exc).__name__}: {exc}\n",
                                encoding="utf-8",
                            )
                            lane_results.append(
                                LaneResult(
                                    lane_id=lane.lane_id,
                                    title=lane.title,
                                    objective=lane.objective,
                                    required=lane.required,
                                    status="failed",
                                    duration_seconds=0,
                                    log_path=str(log_path.relative_to(output_directory)),
                                    log_sha256=hashlib.sha256(log_path.read_bytes()).hexdigest(),
                                    steps=(),
                                )
                            )
            finally:
                cleanup_worktrees(repo_root, worktrees)
                worktrees.clear()
    except Exception as exc:
        findings.append(
            {
                "id": "coordinator-failure",
                "severity": "blocker",
                "message": f"{type(exc).__name__}: {exc}",
            }
        )
    finally:
        cleanup_worktrees(repo_root, worktrees)

    order = {lane.lane_id: index for index, lane in enumerate(plan.lanes)}
    lane_results.sort(key=lambda result: order[result.lane_id])
    completed = dt.datetime.now(dt.timezone.utc)
    json_path, markdown_path, verdict = write_reports(
        output_directory=output_directory,
        run_id=run_id,
        started_at=started.isoformat(),
        completed_at=completed.isoformat(),
        sha=sha,
        version=version,
        findings=findings,
        lane_results=lane_results,
    )

    latest_root = repo_root / ".ashlar" / "release-manager"
    latest_root.mkdir(parents=True, exist_ok=True)
    shutil.copy2(json_path, latest_root / "latest.json")
    shutil.copy2(markdown_path, latest_root / "latest.md")

    print(f"release-manager: verdict={verdict}")
    print(f"release-manager: report={markdown_path}")
    print(f"release-manager: json={json_path}")
    return 0 if verdict == "ready" else 2


if __name__ == "__main__":
    raise SystemExit(main())
