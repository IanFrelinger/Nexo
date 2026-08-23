#!/usr/bin/env python3
"""Align C# namespaces with folder paths (IDE0130), excluding intentional shims."""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SKIP_PARTS = {"obj", "bin", "node_modules", "artifacts"}
EXCLUDE_PATH_PREFIXES = (
    "src/Ashlar.Brick.Contracts/Authoring/",
    "samples/templates/",
    "tools/unity-demo-output/",
    "src/Ashlar.Infrastructure/Execution/Templates/",
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
        ("Ashlar.BrickContracts.Capabilities", "Ashlar.Brick.Contracts.Capabilities"),
        ("Ashlar.BrickContracts", "Ashlar.Brick.Contracts"),
        ("Ashlar.Core.Application.Networking.Models", "Ashlar.Commercial.Fleet.Contracts.Networking.Models"),
        ("Ashlar.Core.Application.Networking.Ports", "Ashlar.Commercial.Fleet.Contracts.Networking.Ports"),
        ("Ashlar.Infrastructure.Networking", "Ashlar.Commercial.Fleet.Infrastructure.Networking"),
        ("Ashlar.Tests.GameDomain.Discord", "Ashlar.Commercial.Tests.GameDomain.Discord"),
        ("Ashlar.Tests.GameDomain.Playtest", "Ashlar.Commercial.Tests.GameDomain.Playtest"),
        ("Ashlar.Tests.GameDomain", "Ashlar.Commercial.Tests.GameDomain"),
        ("Ashlar.API.Forge", "GameDirector.Mcp.Forge"),
        ("Ashlar.API.Endpoints", "GameDirector.Mcp.Endpoints"),
        ("Ashlar.GameDomain", "Ashlar.Commercial.GameDomain"),
        ("Ashlar.Tests.Infrastructure.Tests.Sdk", "Ashlar.Tests.Infrastructure.Tests.SDK"),
        ("Ashlar.Tests.Infrastructure.AshlarClientInvokeTests", "Ashlar.Tests.Infrastructure.Tests.Client"),
        ("Ashlar.Infrastructure.Sdk.Adaptation", "Ashlar.Infrastructure.Adaptation.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Sdk.Analysis", "Ashlar.Infrastructure.Analysis.BrickAnalyzer.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Sdk.Composition", "Ashlar.Infrastructure.Composition.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Execution.Routing.Sdk", "Ashlar.Infrastructure.Execution.Routing.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Execution.Sdk", "Ashlar.Infrastructure.Execution.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Sdk.Maintenance", "Ashlar.Infrastructure.Maintenance.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Mesh.Sdk", "Ashlar.Infrastructure.Mesh.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Sdk.ModelArtifacts", "Ashlar.Infrastructure.ModelArtifacts.Sdk.Extensions"),
        ("Ashlar.Infrastructure.NodeCapabilityRuntime.Sdk", "Ashlar.Infrastructure.NodeCapabilityRuntime.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Sdk.Observation", "Ashlar.Infrastructure.Observation.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Sdk.ParallelTesting", "Ashlar.Infrastructure.ParallelTesting.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Sdk.Persistence", "Ashlar.Infrastructure.Persistence.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Sdk.Pipelines", "Ashlar.Infrastructure.Pipelines.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Sdk.Rollback", "Ashlar.Infrastructure.Rollback.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Sdk.SelfContext", "Ashlar.Infrastructure.SelfContext.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Sdk.SelfImprovement", "Ashlar.Infrastructure.SelfImprovement.Sdk.Extensions"),
        ("Ashlar.Infrastructure.Sdk.Trust", "Ashlar.Infrastructure.Trust.Sdk.Extensions"),
        ("Ashlar.Hosting.Sdk.Options", "Ashlar.Hosting.Sdk.Options"),
        ("Ashlar.Orchestration.Agents.Templates", "Ashlar.Orchestration.Agents.Templates"),
        ("GeneratedBricks", "Ashlar.Certified.DamageResolver"),
    ]
    for old, new in pairs:
        if old != new:
            text = text.replace(old, new)
    return text


def apply_commercial_execution_replacements(text: str, rel_posix: str) -> str:
    if rel_posix.startswith(
        ("commercial/src/Ashlar.Commercial.Fleet/", "commercial/tests/Ashlar.Commercial.Tests.Fleet/")
    ):
        text = text.replace(
            "Ashlar.Infrastructure.Execution",
            "Ashlar.Commercial.Fleet.Infrastructure.Execution",
        )
    return text


def patch_hosting_sdk_namespaces(text: str, rel_posix: str) -> str:
    if not rel_posix.startswith("src/Ashlar.Hosting/Sdk/"):
        return text
    if rel_posix.startswith("src/Ashlar.Hosting/Sdk/Options/"):
        return replace_namespace_declaration(text, "Ashlar.Hosting.Sdk.Options")
    if rel_posix.startswith("src/Ashlar.Hosting/Sdk/Extensions/"):
        return replace_namespace_declaration(text, "Ashlar.Hosting.Sdk.Extensions")
    if rel_posix.startswith("src/Ashlar.Hosting/Sdk/Builders/"):
        return replace_namespace_declaration(text, "Ashlar.Hosting.Sdk.Builders")
    return text


def patch_pipelines_options(text: str, rel_posix: str) -> str:
    if rel_posix.startswith("src/Ashlar.Infrastructure/Pipelines/Sdk/Options/"):
        return replace_namespace_declaration(text, "Ashlar.Infrastructure.Pipelines.Sdk.Options")
    return text


def patch_orchestration_templates(text: str, rel_posix: str) -> str:
    if rel_posix.startswith("src/Ashlar.Orchestration/Agents/Templates/"):
        return replace_namespace_declaration(text, "Ashlar.Orchestration.Agents.Templates")
    return text


def patch_cli_subfolders(text: str, rel_posix: str) -> str:
    if rel_posix.startswith("application/src/Ashlar.CLI/Commands/Runtime/"):
        return replace_namespace_declaration(text, "Ashlar.CLI.Commands.Runtime")
    if rel_posix.startswith("application/src/Ashlar.CLI/Commands/Workflow/"):
        return replace_namespace_declaration(text, "Ashlar.CLI.Commands.Workflow")
    if rel_posix.startswith("application/src/Ashlar.CLI/Commands/Unity/"):
        return replace_namespace_declaration(text, "Ashlar.CLI.Commands.Unity")
    return text


def patch_sdk_legacy(text: str, rel_posix: str) -> str:
    if rel_posix == "src/Ashlar.Sdk/Legacy/AshlarSdkLegacyApiAliases.cs":
        return replace_namespace_declaration(text, "Ashlar.Sdk.Legacy")
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
