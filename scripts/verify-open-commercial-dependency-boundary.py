#!/usr/bin/env python3
"""
Verify the Nexo open-core / commercial project boundary.

Checks:
  1. No OPEN .csproj may ProjectReference a COMMERCIAL .csproj.
  2. Every COMMERCIAL .csproj directory contains COMMERCIAL-LICENSE.md.
  3. Every OPEN packable .csproj resolves PackageLicenseExpression=Apache-2.0.

Optional allowlists (repo-relative paths, # comments allowed):
  scripts/dependency-boundary.open-to-commercial.allowlist.txt
    Lines: "<open_csproj> -> <commercial_csproj>"

Environment:
  DEPENDENCY_BOUNDARY_STRICT=1  — treat license/metadata warnings as errors (default in CI).

Exit 0 on success, 1 on failure.
"""
from __future__ import annotations

import os
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path


EXCLUDED_CSPROJ: set[str] = {
    "src/Nexo.Tests.Infrastructure/scripts/copy-assemblies.csproj",
}

COMMERCIAL_PATH_MARKERS = (
    "commercial/",
)

OPEN_LICENSE_ALLOWLIST: set[str] = {
    # Effective license is Apache-2.0 via Directory.Build.targets; listed for documentation only.
}

PROJECT_REF_RE = re.compile(
    r'<ProjectReference\s+Include="([^"]+)"',
    re.IGNORECASE,
)

NEXO_COMMERCIAL_RE = re.compile(
    r"<NexoCommercialProject>\s*true\s*</NexoCommercialProject>",
    re.IGNORECASE,
)

IS_PACKABLE_TRUE_RE = re.compile(
    r"<IsPackable>\s*true\s*</IsPackable>",
    re.IGNORECASE,
)


@dataclass(frozen=True)
class ProjectInfo:
    rel: str
    path: Path
    commercial: bool
    packable: bool


def repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def rel_posix(path: Path, root: Path) -> str:
    return path.resolve().relative_to(root.resolve()).as_posix()


def read_allowlist(root: Path) -> set[tuple[str, str]]:
    path = root / "scripts/dependency-boundary.open-to-commercial.allowlist.txt"
    if not path.is_file():
        return set()
    pairs: set[tuple[str, str]] = set()
    for line in path.read_text(encoding="utf-8").splitlines():
        body = line.split("#", 1)[0].strip()
        if not body or "->" not in body:
            continue
        left, right = (part.strip() for part in body.split("->", 1))
        pairs.add((left.replace("\\", "/"), right.replace("\\", "/")))
    return pairs


def discover_csprojs(root: Path) -> list[Path]:
    out: list[Path] = []
    for path in sorted(root.rglob("*.csproj")):
        rel = rel_posix(path, root)
        if rel in EXCLUDED_CSPROJ:
            continue
        if "/bin/" in rel or "/obj/" in rel:
            continue
        out.append(path)
    return out


def classify_project(path: Path, root: Path) -> ProjectInfo:
    rel = rel_posix(path, root)
    text = path.read_text(encoding="utf-8", errors="replace")
    commercial = any(marker in rel for marker in COMMERCIAL_PATH_MARKERS)
    commercial = commercial or bool(NEXO_COMMERCIAL_RE.search(text))
    packable = bool(IS_PACKABLE_TRUE_RE.search(text))
    return ProjectInfo(rel=rel, path=path, commercial=commercial, packable=packable)


def parse_project_references(project: ProjectInfo, root: Path) -> list[str]:
    text = project.path.read_text(encoding="utf-8", errors="replace")
    refs: list[str] = []
    for raw in PROJECT_REF_RE.findall(text):
        normalized = raw.replace("\\", "/")
        ref_path = (project.path.parent / normalized).resolve()
        if not ref_path.is_file():
            raise FileNotFoundError(f"{project.rel}: ProjectReference not found: {raw}")
        refs.append(rel_posix(ref_path, root))
    return refs


def msbuild_property(csproj: Path, name: str) -> str:
    cmd = [
        "dotnet",
        "msbuild",
        str(csproj),
        f"-getProperty:{name}",
        "-nologo",
        "-verbosity:quiet",
    ]
    return subprocess.check_output(cmd, text=True).strip()


def main() -> int:
    root = repo_root()
    strict = os.environ.get("DEPENDENCY_BOUNDARY_STRICT", "0") == "1"
    allowlisted_edges = read_allowlist(root)

    errors: list[str] = []
    warnings: list[str] = []

    projects = [classify_project(path, root) for path in discover_csprojs(root)]
    by_rel = {p.rel: p for p in projects}

    # 1) open -> commercial references
    for project in projects:
        if project.commercial:
            continue
        try:
            refs = parse_project_references(project, root)
        except FileNotFoundError as ex:
            errors.append(str(ex))
            continue
        for ref_rel in refs:
            target = by_rel.get(ref_rel)
            if target is None:
                warnings.append(f"unclassified reference target: {project.rel} -> {ref_rel}")
                continue
            if not target.commercial:
                continue
            edge = (project.rel, ref_rel)
            if edge in allowlisted_edges:
                continue
            errors.append(
                f"open project references commercial project: {project.rel} -> {ref_rel}"
            )

    # 2) commercial license stubs
    for project in projects:
        if not project.commercial:
            continue
        license_path = project.path.parent / "COMMERCIAL-LICENSE.md"
        if not license_path.is_file():
            msg = f"commercial project missing COMMERCIAL-LICENSE.md: {project.rel}"
            if strict:
                errors.append(msg)
            else:
                warnings.append(msg)

    # 3) open packable Apache-2.0 metadata
    for project in projects:
        if project.commercial or not project.packable:
            continue
        if project.rel in OPEN_LICENSE_ALLOWLIST:
            continue
        try:
            license_expr = msbuild_property(project.path, "PackageLicenseExpression")
        except subprocess.CalledProcessError as ex:
            errors.append(f"failed to evaluate PackageLicenseExpression for {project.rel}: {ex}")
            continue
        if license_expr != "Apache-2.0":
            msg = (
                f"open packable project must resolve PackageLicenseExpression=Apache-2.0 "
                f"(got '{license_expr or '<empty>'}'): {project.rel}"
            )
            if strict:
                errors.append(msg)
            else:
                warnings.append(msg)

    open_count = sum(1 for p in projects if not p.commercial)
    commercial_count = sum(1 for p in projects if p.commercial)
    packable_open = sum(1 for p in projects if p.packable and not p.commercial)

    print(f"dependency-boundary: scanned {len(projects)} projects "
          f"({open_count} open, {commercial_count} commercial, {packable_open} open packable)")

    for warning in warnings:
        print(f"dependency-boundary: WARNING: {warning}")

    for error in errors:
        print(f"dependency-boundary: ERROR: {error}", file=sys.stderr)

    if errors:
        print("dependency-boundary: FAILED", file=sys.stderr)
        return 1

    if warnings and not strict:
        print(f"dependency-boundary: PASS with {len(warnings)} warning(s)")
    else:
        print("dependency-boundary: PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
