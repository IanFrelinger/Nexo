#!/usr/bin/env python3
"""Align C# namespaces with folder paths (IDE0130), excluding intentional shims."""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SKIP_PARTS = {"obj", "bin", "node_modules", "artifacts"}
EXCLUDE_PATH_PREFIXES = (
    "src/Nexo.Brick.Contracts/Authoring/",
    "samples/templates/",
    "tools/unity-demo-output/",
    "src/Nexo.Infrastructure/Execution/Templates/",
)

NS_DECL = re.compile(
    r"^(\s*namespace\s+)([\w.]+)(\s*(?:;|\{))\s*$",
    re.MULTILINE,
)


def get_root_namespace(csproj: Path) -> str:
    text = csproj.read_text(encoding="utf-8")
    match = re.search(r"<RootNamespace>([^<]+)</RootNamespace>", text)
    return match.group(1).strip() if match else csproj.stem


def expected_namespace(csproj: Path, cs_file: Path) -> str:
    project_dir = csproj.parent
    root_ns = get_root_namespace(csproj)
    rel = cs_file.relative_to(project_dir)
    sub = ".".join(rel.parts[:-1])
    return root_ns if not sub else f"{root_ns}.{sub.replace('/', '.')}"


def actual_namespace(text: str) -> str | None:
    match = re.search(r"^\s*namespace\s+([\w.]+)\s*;", text, re.MULTILINE)
    if match:
        return match.group(1)
    match = re.search(r"^\s*namespace\s+([\w.]+)\s*\{", text, re.MULTILINE)
    return match.group(1) if match else None


def is_excluded(rel_posix: str) -> bool:
    return any(rel_posix.startswith(prefix) for prefix in EXCLUDE_PATH_PREFIXES)


def collect_mismatches() -> list[tuple[str, str, str]]:
    rows: list[tuple[str, str, str]] = []
    for csproj in sorted(ROOT.rglob("*.csproj")):
        if any(part in SKIP_PARTS for part in csproj.parts):
            continue
        rel_csproj = csproj.relative_to(ROOT).as_posix()
        if rel_csproj.startswith("artifacts/"):
            continue
        project_dir = csproj.parent
        for cs in sorted(project_dir.rglob("*.cs")):
            if any(part in SKIP_PARTS for part in cs.parts):
                continue
            rel = cs.relative_to(ROOT).as_posix()
            if is_excluded(rel):
                continue
            text = cs.read_text(encoding="utf-8", errors="ignore")
            actual = actual_namespace(text)
            if not actual:
                continue
            expected = expected_namespace(csproj, cs)
            if actual != expected:
                rows.append((rel, actual, expected))
    return rows


def replace_namespace_declaration(text: str, expected: str) -> str:
    def repl(match: re.Match[str]) -> str:
        return f"{match.group(1)}{expected}{match.group(3)}"

    return NS_DECL.sub(repl, text, count=1)


def apply_global_replacements(text: str) -> str:
    pairs = [
        ("Nexo.BrickContracts.Capabilities", "Nexo.Brick.Contracts.Capabilities"),
        ("Nexo.BrickContracts", "Nexo.Brick.Contracts"),
        ("Nexo.Core.Application.Networking.Models", "Nexo.Commercial.Fleet.Contracts.Networking.Models"),
        ("Nexo.Core.Application.Networking.Ports", "Nexo.Commercial.Fleet.Contracts.Networking.Ports"),
        ("Nexo.Infrastructure.Networking", "Nexo.Commercial.Fleet.Infrastructure.Networking"),
        ("Nexo.Tests.GameDomain.Discord", "Nexo.Commercial.Tests.GameDomain.Discord"),
        ("Nexo.Tests.GameDomain.Playtest", "Nexo.Commercial.Tests.GameDomain.Playtest"),
        ("Nexo.Tests.GameDomain", "Nexo.Commercial.Tests.GameDomain"),
        ("Nexo.API.Forge", "GameDirector.Mcp.Forge"),
        ("Nexo.API.Endpoints", "GameDirector.Mcp.Endpoints"),
        ("Nexo.GameDomain", "Nexo.Commercial.GameDomain"),
        ("Nexo.Tests.Infrastructure.Tests.Sdk", "Nexo.Tests.Infrastructure.Tests.SDK"),
        ("Nexo.Tests.Infrastructure.NexoClientInvokeTests", "Nexo.Tests.Infrastructure.Tests.Client"),
        ("Nexo.Infrastructure.Sdk.Adaptation", "Nexo.Infrastructure.Adaptation.Sdk.Extensions"),
        ("Nexo.Infrastructure.Sdk.Analysis", "Nexo.Infrastructure.Analysis.BrickAnalyzer.Sdk.Extensions"),
        ("Nexo.Infrastructure.Sdk.Composition", "Nexo.Infrastructure.Composition.Sdk.Extensions"),
        ("Nexo.Infrastructure.Execution.Routing.Sdk", "Nexo.Infrastructure.Execution.Routing.Sdk.Extensions"),
        ("Nexo.Infrastructure.Execution.Sdk", "Nexo.Infrastructure.Execution.Sdk.Extensions"),
        ("Nexo.Infrastructure.Sdk.Maintenance", "Nexo.Infrastructure.Maintenance.Sdk.Extensions"),
        ("Nexo.Infrastructure.Mesh.Sdk", "Nexo.Infrastructure.Mesh.Sdk.Extensions"),
        ("Nexo.Infrastructure.Sdk.ModelArtifacts", "Nexo.Infrastructure.ModelArtifacts.Sdk.Extensions"),
        ("Nexo.Infrastructure.NodeCapabilityRuntime.Sdk", "Nexo.Infrastructure.NodeCapabilityRuntime.Sdk.Extensions"),
        ("Nexo.Infrastructure.Sdk.Observation", "Nexo.Infrastructure.Observation.Sdk.Extensions"),
        ("Nexo.Infrastructure.Sdk.ParallelTesting", "Nexo.Infrastructure.ParallelTesting.Sdk.Extensions"),
        ("Nexo.Infrastructure.Sdk.Persistence", "Nexo.Infrastructure.Persistence.Sdk.Extensions"),
        ("Nexo.Infrastructure.Sdk.Pipelines", "Nexo.Infrastructure.Pipelines.Sdk.Extensions"),
        ("Nexo.Infrastructure.Sdk.Rollback", "Nexo.Infrastructure.Rollback.Sdk.Extensions"),
        ("Nexo.Infrastructure.Sdk.SelfContext", "Nexo.Infrastructure.SelfContext.Sdk.Extensions"),
        ("Nexo.Infrastructure.Sdk.SelfImprovement", "Nexo.Infrastructure.SelfImprovement.Sdk.Extensions"),
        ("Nexo.Infrastructure.Sdk.Trust", "Nexo.Infrastructure.Trust.Sdk.Extensions"),
        ("Nexo.Hosting.Sdk.Options", "Nexo.Hosting.Sdk.Options"),
        ("Nexo.Orchestration.Agents.Templates", "Nexo.Orchestration.Agents.Templates"),
        ("GeneratedBricks", "Nexo.Certified.DamageResolver"),
    ]
    for old, new in pairs:
        if old != new:
            text = text.replace(old, new)
    return text


def apply_commercial_execution_replacements(text: str, rel_posix: str) -> str:
    if rel_posix.startswith(
        ("commercial/src/Nexo.Commercial.Fleet/", "commercial/tests/Nexo.Commercial.Tests.Fleet/")
    ):
        text = text.replace(
            "Nexo.Infrastructure.Execution",
            "Nexo.Commercial.Fleet.Infrastructure.Execution",
        )
    return text


def patch_hosting_sdk_namespaces(text: str, rel_posix: str) -> str:
    if not rel_posix.startswith("src/Nexo.Hosting/Sdk/"):
        return text
    if rel_posix.startswith("src/Nexo.Hosting/Sdk/Options/"):
        return replace_namespace_declaration(text, "Nexo.Hosting.Sdk.Options")
    if rel_posix.startswith("src/Nexo.Hosting/Sdk/Extensions/"):
        return replace_namespace_declaration(text, "Nexo.Hosting.Sdk.Extensions")
    if rel_posix.startswith("src/Nexo.Hosting/Sdk/Builders/"):
        return replace_namespace_declaration(text, "Nexo.Hosting.Sdk.Builders")
    return text


def patch_pipelines_options(text: str, rel_posix: str) -> str:
    if rel_posix.startswith("src/Nexo.Infrastructure/Pipelines/Sdk/Options/"):
        return replace_namespace_declaration(text, "Nexo.Infrastructure.Pipelines.Sdk.Options")
    return text


def patch_orchestration_templates(text: str, rel_posix: str) -> str:
    if rel_posix.startswith("src/Nexo.Orchestration/Agents/Templates/"):
        return replace_namespace_declaration(text, "Nexo.Orchestration.Agents.Templates")
    return text


def patch_cli_subfolders(text: str, rel_posix: str) -> str:
    if rel_posix.startswith("application/src/Nexo.CLI/Commands/Runtime/"):
        return replace_namespace_declaration(text, "Nexo.CLI.Commands.Runtime")
    if rel_posix.startswith("application/src/Nexo.CLI/Commands/Workflow/"):
        return replace_namespace_declaration(text, "Nexo.CLI.Commands.Workflow")
    if rel_posix.startswith("application/src/Nexo.CLI/Commands/Unity/"):
        return replace_namespace_declaration(text, "Nexo.CLI.Commands.Unity")
    return text


def patch_sdk_legacy(text: str, rel_posix: str) -> str:
    if rel_posix == "src/Nexo.Sdk/Legacy/NexoSdkLegacyApiAliases.cs":
        return replace_namespace_declaration(text, "Nexo.Sdk.Legacy")
    return text


TEXT_EXTENSIONS = {".cs", ".txt", ".sh", ".md", ".json", ".props", ".targets", ".csproj"}


def main() -> None:
    mismatches = collect_mismatches()
    print(f"Fixing {len(mismatches)} namespace declarations...")

    per_file_expected = {rel: expected for rel, _, expected in mismatches}

    for rel, actual, expected in mismatches:
        path = ROOT / rel
        text = path.read_text(encoding="utf-8")
        updated = replace_namespace_declaration(text, expected)
        if updated != text:
            path.write_text(updated, encoding="utf-8", newline="\n")
            print(f"  {rel}: {actual} -> {expected}")

    for path in sorted(ROOT.rglob("*")):
        if not path.is_file():
            continue
        if any(part in SKIP_PARTS for part in path.parts):
            continue
        rel = path.relative_to(ROOT).as_posix()
        if rel.startswith("artifacts/"):
            continue
        if path.suffix not in TEXT_EXTENSIONS:
            continue
        text = path.read_text(encoding="utf-8", errors="ignore")
        original = text
        text = apply_global_replacements(text)
        text = apply_commercial_execution_replacements(text, rel)
        text = patch_hosting_sdk_namespaces(text, rel)
        text = patch_pipelines_options(text, rel)
        text = patch_orchestration_templates(text, rel)
        text = patch_cli_subfolders(text, rel)
        text = patch_sdk_legacy(text, rel)
        if rel in per_file_expected and path.suffix == ".cs":
            text = replace_namespace_declaration(text, per_file_expected[rel])
        if text != original:
            path.write_text(text, encoding="utf-8", newline="\n")

    remaining = collect_mismatches()
    print(f"Remaining mismatches (excluding shims): {len(remaining)}")
    for rel, actual, expected in remaining[:20]:
        print(f"  {rel}: {actual} -> {expected}")


if __name__ == "__main__":
    main()
