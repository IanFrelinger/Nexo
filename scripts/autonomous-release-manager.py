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
import fcntl
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
import uuid
from pathlib import Path
from typing import Any


SCHEMA_VERSION = 1
CANONICAL_PLAN_RELATIVE_PATH = "ci/autonomous-release-manager.json"
TRUSTED_PLAN_SHA256 = "638a2ae47cb355a2e791f5f4840d188c054d6209d99e8a1cfb8cb2a10ad518c7"
MAX_AUDIT_EXECUTION_SECONDS = 18_000
PROCESS_TERMINATION_GRACE_SECONDS = 10
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
_SEMVER_CORE = r"(?:0|[1-9][0-9]*)"
_SEMVER_PRERELEASE = r"(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)"
SEMVER_RE = re.compile(
    rf"^{_SEMVER_CORE}\.{_SEMVER_CORE}\.{_SEMVER_CORE}"
    rf"(?:-{_SEMVER_PRERELEASE}(?:\.{_SEMVER_PRERELEASE})*)?"
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
SAFE_INHERITED_ENV = {
    "HTTP_PROXY",
    "HTTPS_PROXY",
    "LANG",
    "LC_ALL",
    "NO_PROXY",
    "SSL_CERT_DIR",
    "SSL_CERT_FILE",
    "TZ",
    "http_proxy",
    "https_proxy",
    "no_proxy",
}
TRUSTED_PATH_DIRECTORIES = (
    "/usr/local/bin",
    "/usr/bin",
    "/bin",
    "/usr/local/sbin",
    "/usr/sbin",
    "/sbin",
)


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
        if any(step.timeout_seconds is None for step in steps):
            raise PlanError(f"lane {lane_id} must set a timeout on every step.")
        step_budget = sum(step.timeout_seconds or 0 for step in steps)
        if step_budget > lane_timeout:
            raise PlanError(
                f"lane {lane_id} step budgets total {step_budget}s, "
                f"exceeding lane timeout {lane_timeout}s."
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

    plan = AuditPlan(max_parallel=max_parallel, lanes=tuple(lanes))
    declared_runtime = declared_schedule_seconds(plan)
    if declared_runtime > MAX_AUDIT_EXECUTION_SECONDS:
        raise PlanError(
            f"Declared audit schedule is {declared_runtime}s; "
            f"maximum is {MAX_AUDIT_EXECUTION_SECONDS}s."
        )
    return plan


def declared_schedule_seconds(plan: AuditPlan) -> int:
    worker_available = [0] * plan.max_parallel
    resource_available: dict[str, int] = {}
    for lane in plan.lanes:
        worker_index = min(
            range(len(worker_available)), key=lambda index: worker_available[index]
        )
        start = worker_available[worker_index]
        for resource in lane.exclusive_resources:
            start = max(start, resource_available.get(resource, 0))
        completed = start + lane.timeout_seconds
        worker_available[worker_index] = completed
        for resource in lane.exclusive_resources:
            resource_available[resource] = completed
    return max(worker_available, default=0)


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


def validate_plan_binding(repo_root: Path, plan_path: Path, sha: str) -> str:
    relative = str(plan_path.relative_to(repo_root)).replace(os.sep, "/")
    if relative != CANONICAL_PLAN_RELATIVE_PATH:
        raise PlanError(
            f"Only the canonical tracked plan is executable: {CANONICAL_PLAN_RELATIVE_PATH}."
        )
    show = subprocess.run(
        ["git", "show", f"{sha}:{relative}"],
        cwd=repo_root,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if show.returncode != 0:
        raise PlanError(f"Canonical plan is not tracked at HEAD: {relative}.")
    current = plan_path.read_bytes()
    if current != show.stdout:
        raise PlanError("Canonical plan differs from the audited HEAD.")
    digest = hashlib.sha256(current).hexdigest()
    if digest != TRUSTED_PLAN_SHA256:
        raise PlanError(
            "Canonical plan digest does not match the code-owned release policy: "
            f"{digest}."
        )
    return digest


def validate_coordinator_binding(repo_root: Path, sha: str) -> str:
    relative = "scripts/autonomous-release-manager.py"
    show = subprocess.run(
        ["git", "show", f"{sha}:{relative}"],
        cwd=repo_root,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if show.returncode != 0:
        raise PlanError(f"Coordinator is not tracked at {sha}.")
    current = Path(__file__).read_bytes()
    if current != show.stdout:
        raise PlanError("Coordinator differs from the audited commit.")
    return hashlib.sha256(current).hexdigest()


def _render(value: str, context: dict[str, str]) -> str:
    rendered = value
    for key, replacement in context.items():
        rendered = rendered.replace("{" + key + "}", replacement)
    if re.search(r"\{[A-Za-z][A-Za-z0-9_]*\}", rendered):
        raise PlanError(f"Unknown template variable in {value!r}.")
    return rendered


def _terminate_process_tree(process: subprocess.Popen[bytes]) -> None:
    if os.name == "nt":
        subprocess.run(
            ["taskkill", "/PID", str(process.pid), "/T", "/F"],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            check=False,
        )
        return

    process_group = process.pid
    try:
        os.killpg(process_group, signal.SIGTERM)
    except ProcessLookupError:
        return

    deadline = time.monotonic() + PROCESS_TERMINATION_GRACE_SECONDS
    while time.monotonic() < deadline:
        try:
            os.killpg(process_group, 0)
        except ProcessLookupError:
            break
        time.sleep(0.05)
    else:
        try:
            os.killpg(process_group, signal.SIGKILL)
        except ProcessLookupError:
            pass

    try:
        process.wait(timeout=5)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait(timeout=5)


def _token_processes(token: str) -> list[int]:
    proc = Path("/proc")
    if not proc.is_dir():
        return []
    needle = f"ASHLAR_AUDIT_STEP_TOKEN={token}".encode()
    matches: list[int] = []
    for entry in proc.iterdir():
        if not entry.name.isdigit():
            continue
        pid = int(entry.name)
        if pid == os.getpid():
            continue
        try:
            environment = (entry / "environ").read_bytes().split(b"\0")
        except (FileNotFoundError, PermissionError, ProcessLookupError):
            continue
        if needle in environment:
            matches.append(pid)
    return matches


def _kill_token_processes(token: str) -> list[int]:
    found: set[int] = set()
    deadline = time.monotonic() + PROCESS_TERMINATION_GRACE_SECONDS
    while True:
        pids = _token_processes(token)
        found.update(pids)
        if not pids:
            return sorted(found)
        for pid in pids:
            try:
                os.kill(pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
        if time.monotonic() >= deadline:
            remaining = _token_processes(token)
            if remaining:
                raise RuntimeError(
                    f"Could not contain descendant processes: {sorted(remaining)}"
                )
            return sorted(found)
        time.sleep(0.02)


def _kill_detached_token_processes(token: str, primary_group: int) -> list[int]:
    killed: list[int] = []
    for pid in _token_processes(token):
        try:
            process_group = os.getpgid(pid)
        except (ProcessLookupError, PermissionError):
            continue
        if process_group == primary_group:
            continue
        try:
            os.kill(pid, signal.SIGKILL)
            killed.append(pid)
        except ProcessLookupError:
            pass
    return killed


def _sanitized_environment(worktree: Path, step_token: str) -> dict[str, str]:
    environment = {
        name: os.environ[name]
        for name in SAFE_INHERITED_ENV
        if name in os.environ
    }
    trusted_paths = list(TRUSTED_PATH_DIRECTORIES)
    dotnet_root = os.environ.get("DOTNET_ROOT")
    if dotnet_root:
        trusted_paths.insert(0, dotnet_root)
    audit_home = worktree / ".ashlar" / "release-manager" / "home"
    nuget_cache = worktree / ".ashlar" / "release-manager" / "nuget"
    audit_home.mkdir(parents=True, exist_ok=True)
    nuget_cache.mkdir(parents=True, exist_ok=True)
    environment.update(
        {
            "ASHLAR_AUDIT_STEP_TOKEN": step_token,
            "CI": "true",
            "DOTNET_CLI_HOME": str(audit_home),
            "DOTNET_CLI_USE_MSBUILD_SERVER": "0",
            "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
            "DOTNET_NOLOGO": "true",
            "GIT_CONFIG_GLOBAL": os.devnull,
            "GIT_CONFIG_NOSYSTEM": "1",
            "HOME": str(audit_home),
            "MSBUILDDISABLENODEREUSE": "1",
            "NUGET_PACKAGES": str(nuget_cache),
            "PATH": os.pathsep.join(trusted_paths),
            "ASHLAR_RELEASE_AUDIT": "1",
            "UseSharedCompilation": "false",
        }
    )
    return environment


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

    step_token = uuid.uuid4().hex
    environment = _sanitized_environment(worktree, step_token)
    environment.update(
        {_render(key, context): _render(value, context) for key, value in step.environment.items()}
    )
    resolved_executable = shutil.which(command[0], path=environment["PATH"])
    if resolved_executable is None:
        return StepResult(
            name=step.name,
            command=command,
            status="error",
            exit_code=None,
            duration_seconds=0,
            timeout_seconds=timeout_seconds,
            error=f"Executable is unavailable on trusted PATH: {command[0]}",
        )
    command = (resolved_executable, *command[1:])

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
        _kill_detached_token_processes(step_token, process.pid)
        _terminate_process_tree(process)
        exit_code = process.poll()
        status = "timeout"
        error = f"Exceeded {timeout_seconds} seconds."
    try:
        escaped_descendants = _kill_token_processes(step_token)
    except RuntimeError as exc:
        escaped_descendants = []
        status = "error"
        error = str(exc)
    if escaped_descendants and status == "passed":
        status = "failed"
        error = (
            "Step left descendant processes after its leader exited; "
            f"contained pids={escaped_descendants}."
        )

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


def _semver_precedence(version: str) -> tuple[Any, ...]:
    core_and_pre = version.split("+", 1)[0]
    core, separator, prerelease = core_and_pre.partition("-")
    major, minor, patch = (int(part) for part in core.split("."))
    if not separator:
        return major, minor, patch, 1, ()
    identifiers: list[tuple[int, Any]] = []
    for part in prerelease.split("."):
        identifiers.append((0, int(part)) if part.isdigit() else (1, part))
    return major, minor, patch, 0, tuple(identifiers)


def repository_findings(repo_root: Path, version: str, sha: str) -> list[dict[str, str]]:
    findings: list[dict[str, str]] = []
    version_path = repo_root / "VERSION"
    published_path = repo_root / "ci" / "published-version"
    changelog_path = repo_root / "CHANGELOG.md"
    required_paths = (version_path, published_path, changelog_path)
    missing_paths = [str(path.relative_to(repo_root)) for path in required_paths if not path.is_file()]
    if missing_paths:
        findings.append(
            {
                "id": "release-state-files-missing",
                "severity": "blocker",
                "message": f"Required release state files are missing: {missing_paths}.",
            }
        )
        return findings

    canonical = version_path.read_text(encoding="utf-8").strip()
    published = published_path.read_text(encoding="utf-8").strip()
    if version != canonical:
        findings.append(
            {
                "id": "version-mismatch",
                "severity": "blocker",
                "message": f"Requested version {version} does not match VERSION ({canonical}).",
            }
        )
    if not SEMVER_RE.fullmatch(version) or not SEMVER_RE.fullmatch(canonical):
        findings.append(
            {
                "id": "invalid-version",
                "severity": "blocker",
                "message": f"Requested/canonical version is not SemVer: {version!r}/{canonical!r}.",
            }
        )
    if not SEMVER_RE.fullmatch(published):
        findings.append(
            {
                "id": "invalid-published-version",
                "severity": "blocker",
                "message": f"ci/published-version is not SemVer: {published!r}.",
            }
        )

    changelog = changelog_path.read_text(encoding="utf-8")
    if changelog.count("## [Unreleased]") != 1:
        findings.append(
            {
                "id": "invalid-unreleased-changelog",
                "severity": "blocker",
                "message": "CHANGELOG.md must contain exactly one ## [Unreleased] section.",
            }
        )
        unreleased = ""
    else:
        after_heading = changelog.split("## [Unreleased]", 1)[1]
        if "\n## [" not in after_heading:
            findings.append(
                {
                    "id": "invalid-unreleased-changelog",
                    "severity": "blocker",
                    "message": "CHANGELOG [Unreleased] has no following release section.",
                }
            )
            unreleased = ""
        else:
            unreleased = after_heading.partition("\n## [")[0]

    meaningful = any(line.startswith("- ") for line in unreleased.splitlines())
    if meaningful and SEMVER_RE.fullmatch(canonical) and SEMVER_RE.fullmatch(published):
        if _semver_precedence(canonical) <= _semver_precedence(published):
            findings.append(
                {
                    "id": "candidate-version-not-advanced",
                    "severity": "blocker",
                    "message": (
                        f"VERSION {canonical} must be greater than published pin {published} "
                        "while CHANGELOG [Unreleased] contains release content."
                    ),
                }
            )
        else:
            findings.append(
                {
                    "id": "uncut-unreleased-changelog",
                    "severity": "blocker",
                    "message": (
                        f"Move CHANGELOG [Unreleased] entries under [{canonical}] "
                        "before declaring the candidate ready."
                    ),
                }
            )
    if SEMVER_RE.fullmatch(canonical) and SEMVER_RE.fullmatch(published):
        if _semver_precedence(canonical) < _semver_precedence(published):
            findings.append(
                {
                    "id": "candidate-version-downgrade",
                    "severity": "blocker",
                    "message": (
                        f"VERSION {canonical} is older than published pin {published}."
                    ),
                }
            )
        if _semver_precedence(canonical) > _semver_precedence(published):
            release_heading = re.compile(
                rf"(?m)^## \[{re.escape(canonical)}\] - [0-9]{{4}}-[0-9]{{2}}-[0-9]{{2}}$"
            )
            if not release_heading.search(changelog):
                findings.append(
                    {
                        "id": "candidate-changelog-section-missing",
                        "severity": "blocker",
                        "message": (
                            f"CHANGELOG.md has no dated [{canonical}] release section."
                        ),
                    }
                )

    status = subprocess.run(
        ["git", "status", "--porcelain", "--untracked-files=all"],
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
                "message": "Release audit must run from a completely clean worktree.",
            }
        )

    if canonical != published and subprocess.run(
        ["git", "rev-parse", "--verify", "--quiet", f"refs/tags/v{canonical}"],
        cwd=repo_root,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        check=False,
    ).returncode == 0:
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


def remove_worktree(repo_root: Path, path: Path) -> str | None:
    result = subprocess.run(
            ["git", "worktree", "remove", "--force", str(path)],
            cwd=repo_root,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=False,
        )
    if result.returncode != 0:
        return result.stderr.strip() or f"git worktree remove exited {result.returncode}"
    return None


def cleanup_worktrees(repo_root: Path, worktrees: dict[str, Path]) -> list[str]:
    errors: list[str] = []
    for path in worktrees.values():
        error = remove_worktree(repo_root, path)
        if error:
            errors.append(f"{path}: {error}")
    return errors


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


def _markdown_inline(value: str) -> str:
    return value.replace("\\", "\\\\").replace("`", "\\`").replace("\r", " ").replace("\n", " ")


def _atomic_write(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.{uuid.uuid4().hex}.tmp")
    temporary.write_text(content, encoding="utf-8")
    os.replace(temporary, path)


def write_reports(
    output_directory: Path,
    run_id: str,
    started_at: str,
    completed_at: str,
    sha: str,
    version: str,
    plan_sha256: str,
    coordinator_sha256: str,
    findings: list[dict[str, str]],
    lane_results: list[LaneResult],
) -> tuple[Path, Path, str]:
    result_ids = [result.lane_id for result in lane_results]
    if len(result_ids) != len(CANONICAL_LANES) or set(result_ids) != CANONICAL_LANES:
        findings = [
            *findings,
            {
                "id": "incomplete-lane-results",
                "severity": "blocker",
                "message": (
                    f"Expected results for {sorted(CANONICAL_LANES)}; "
                    f"received {sorted(result_ids)}."
                ),
            },
        ]
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
        "planSha256": plan_sha256,
        "coordinatorSha256": coordinator_sha256,
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
    _atomic_write(json_path, json.dumps(report, indent=2, sort_keys=True) + "\n")

    lines = [
        "# Autonomous release-manager report",
        "",
        f"- Verdict: **{verdict.upper()}**",
        f"- Version: `{_markdown_inline(version)}`",
        f"- Commit: `{_markdown_inline(sha)}`",
        f"- Plan: `sha256:{plan_sha256}`",
        f"- Coordinator: `sha256:{coordinator_sha256}`",
        f"- Run: `{_markdown_inline(run_id)}`",
        "",
        "## Repository findings",
        "",
    ]
    for finding in findings:
        lines.append(
            f"- **{_markdown_inline(finding['severity'].upper())}** "
            f"`{_markdown_inline(finding['id'])}` — "
            f"{_markdown_inline(finding['message'])}"
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
            f"| `{_markdown_inline(result.lane_id)}` | "
            f"**{_markdown_inline(result.status.upper())}** | "
            f"{result.duration_seconds:.1f}s | `{_markdown_inline(result.log_path)}` "
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
                f"- `{_markdown_inline(result.lane_id)}/{_markdown_inline(step.name)}` "
                f"— {_markdown_inline(step.status)}; exit={step.exit_code}; "
                f"{_markdown_inline(step.error or 'see lane log')}"
            )
    else:
        lines.append("- None.")
    markdown_path = output_directory / "report.md"
    _atomic_write(markdown_path, "\n".join(lines) + "\n")
    return json_path, markdown_path, verdict


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Run all canonical release audit sub-agents and emit a fail-closed verdict."
    )
    parser.add_argument(
        "--plan",
        default=CANONICAL_PLAN_RELATIVE_PATH,
        help=f"Canonical tracked audit plan ({CANONICAL_PLAN_RELATIVE_PATH}).",
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
        sha = _git(repo_root, "rev-parse", "HEAD")
        plan_path = (repo_root / args.plan).resolve()
        plan_path.relative_to(repo_root)
        plan_sha256 = validate_plan_binding(repo_root, plan_path, sha)
        coordinator_sha256 = validate_coordinator_binding(repo_root, sha)
        plan = load_plan(plan_path)
        validate_cursor_assets(repo_root)
    except (PlanError, RuntimeError, ValueError) as exc:
        print(f"release-manager: invalid plan: {exc}", file=sys.stderr)
        return 64

    if args.validate_only:
        print(
            f"release-manager: plan valid (sha256:{plan_sha256}); canonical required lanes="
            + ",".join(sorted(CANONICAL_LANES))
        )
        return 0

    version_path = repo_root / "VERSION"
    canonical_version = (
        version_path.read_text(encoding="utf-8").strip()
        if version_path.is_file()
        else ""
    )
    requested_version = args.version or canonical_version
    version = requested_version[1:] if requested_version[:1] in {"v", "V"} else requested_version
    started = dt.datetime.now(dt.timezone.utc)
    run_id = f"{started.strftime('%Y%m%dT%H%M%SZ')}-{sha[:12]}-{uuid.uuid4().hex[:8]}"
    output_directory = (
        (repo_root / args.output).resolve()
        if args.output
        else repo_root / ".ashlar" / "release-manager" / "runs" / run_id
    )
    output_directory.mkdir(parents=True, exist_ok=True)
    findings = repository_findings(repo_root, version, sha)
    pre_execution_blockers = {
        "dirty-release-sha",
        "invalid-published-version",
        "invalid-unreleased-changelog",
        "invalid-version",
        "release-state-files-missing",
        "version-mismatch",
    }
    may_execute = not any(
        finding["severity"] == "blocker" and finding["id"] in pre_execution_blockers
        for finding in findings
    )

    lane_results: list[LaneResult] = []
    worktrees: dict[str, Path] = {}
    try:
        if not may_execute:
            raise PlanError("Repository state is unsafe; audit commands were not started.")
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
                                "run_id": run_id,
                                "run_slug": f"ashlar-audit-{run_id[-8:]}",
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
                        worktree = worktrees.pop(lane.lane_id)
                        cleanup_error = remove_worktree(repo_root, worktree)
                        if cleanup_error:
                            findings.append(
                                {
                                    "id": f"worktree-cleanup-{lane.lane_id}",
                                    "severity": "blocker",
                                    "message": cleanup_error,
                                }
                            )
            finally:
                for cleanup_error in cleanup_worktrees(repo_root, worktrees):
                    findings.append(
                        {
                            "id": "worktree-cleanup",
                            "severity": "blocker",
                            "message": cleanup_error,
                        }
                    )
                worktrees.clear()
    except PlanError as exc:
        findings.append(
            {
                "id": "commands-not-started",
                "severity": "info",
                "message": str(exc),
            }
        )
    except Exception as exc:
        findings.append(
            {
                "id": "coordinator-failure",
                "severity": "blocker",
                "message": f"{type(exc).__name__}: {exc}",
            }
        )
    finally:
        for cleanup_error in cleanup_worktrees(repo_root, worktrees):
            findings.append(
                {
                    "id": "worktree-cleanup",
                    "severity": "blocker",
                    "message": cleanup_error,
                }
            )

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
        plan_sha256=plan_sha256,
        coordinator_sha256=coordinator_sha256,
        findings=findings,
        lane_results=lane_results,
    )

    latest_root = repo_root / ".ashlar" / "release-manager"
    latest_root.mkdir(parents=True, exist_ok=True)
    try:
        json_reference = str(json_path.relative_to(latest_root))
        markdown_reference = str(markdown_path.relative_to(latest_root))
    except ValueError:
        json_reference = str(json_path)
        markdown_reference = str(markdown_path)
    latest = {
        "schemaVersion": SCHEMA_VERSION,
        "runId": run_id,
        "headSha": sha,
        "version": version,
        "verdict": verdict,
        "reportPath": json_reference,
        "markdownPath": markdown_reference,
    }
    with (latest_root / ".latest.lock").open("a+", encoding="utf-8") as latest_lock:
        fcntl.flock(latest_lock.fileno(), fcntl.LOCK_EX)
        _atomic_write(
            latest_root / "latest.json",
            json.dumps(latest, indent=2, sort_keys=True) + "\n",
        )
        _atomic_write(
            latest_root / "latest.md",
            (
                "# Latest autonomous release-manager run\n\n"
                f"- Verdict: **{verdict.upper()}**\n"
                f"- Commit: `{_markdown_inline(sha)}`\n"
                f"- Report: `{_markdown_inline(markdown_reference)}`\n"
            ),
        )
        fcntl.flock(latest_lock.fileno(), fcntl.LOCK_UN)

    print(f"release-manager: verdict={verdict}")
    print(f"release-manager: report={markdown_path}")
    print(f"release-manager: json={json_path}")
    return 0 if verdict == "ready" else 2


if __name__ == "__main__":
    raise SystemExit(main())
