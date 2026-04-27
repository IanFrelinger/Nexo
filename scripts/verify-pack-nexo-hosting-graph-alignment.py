#!/usr/bin/env python3
"""Compare pack-nexo-hosting-graph.{sh,ps1} to transitive Nexo.* refs from Nexo.Hosting (+ optional allowlist)."""
from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path


def repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def read_allowlist(root: Path) -> set[str]:
    path = root / "scripts/pack-nexo-hosting-graph.allowlist.txt"
    if not path.is_file():
        return set()
    out: set[str] = set()
    for line in path.read_text(encoding="utf-8").splitlines():
        s = line.split("#", 1)[0].strip()
        if s:
            out.add(s.replace("\\", "/"))
    return out


def extract_from_sh(root: Path) -> list[str]:
    text = (root / "scripts/pack-nexo-hosting-graph.sh").read_text(encoding="utf-8")
    paths: list[str] = []
    for line in text.splitlines():
        m = re.match(r"^pack\s+(\S+\.csproj)\s*$", line.strip())
        if m:
            paths.append(m.group(1).replace("\\", "/"))
    return sorted(paths)


def extract_from_ps1(root: Path) -> list[str]:
    text = (root / "scripts/pack-nexo-hosting-graph.ps1").read_text(encoding="utf-8")
    paths: list[str] = []
    for line in text.splitlines():
        m = re.search(r'Pack-Project\s+"([^"]+\.csproj)"', line)
        if m:
            paths.append(m.group(1).replace("\\", "/"))
    return sorted(paths)


def msbuild_project_refs(csproj: Path) -> list[Path]:
    cmd = [
        "dotnet",
        "msbuild",
        str(csproj),
        "-getItem:ProjectReference",
        "-nologo",
        "-verbosity:quiet",
    ]
    raw = subprocess.check_output(cmd, text=True)
    data = json.loads(raw)
    items = (data.get("Items") or {}).get("ProjectReference") or []
    out: list[Path] = []
    for it in items:
        fp = it.get("FullPath") or it.get("Identity")
        if fp:
            out.append(Path(fp).resolve())
    return out


def transitive_nexo_csprojs(root: Path, start: Path) -> set[str]:
    rel_set: set[str] = set()
    queue: list[Path] = [start.resolve()]
    seen: set[Path] = set()
    while queue:
        p = queue.pop()
        if p in seen:
            continue
        seen.add(p)
        name = p.name
        if not name.startswith("Nexo.") or not name.endswith(".csproj"):
            continue
        try:
            rel = p.relative_to(root.resolve())
        except ValueError:
            continue
        rel_set.add(rel.as_posix())
        for child in msbuild_project_refs(p):
            queue.append(child)
    return rel_set


def main() -> int:
    root = repo_root()
    sh_paths = set(extract_from_sh(root))
    ps1_paths = set(extract_from_ps1(root))
    if sh_paths != ps1_paths:
        print("pack-nexo-hosting-graph.sh and .ps1 disagree:", file=sys.stderr)
        for p in sorted(sh_paths - ps1_paths):
            print(f"  only .sh: {p}", file=sys.stderr)
        for p in sorted(ps1_paths - sh_paths):
            print(f"  only .ps1: {p}", file=sys.stderr)
        return 1

    hosting = root / "src/Nexo.Hosting/Nexo.Hosting.csproj"
    if not hosting.is_file():
        print(f"Missing {hosting}", file=sys.stderr)
        return 1

    msbuild_set = transitive_nexo_csprojs(root, hosting)
    allow_extra = read_allowlist(root)
    expected = msbuild_set | allow_extra

    missing = sorted(msbuild_set - sh_paths)
    if missing:
        print("MSBuild graph not in pack script:", file=sys.stderr)
        for p in missing:
            print(f"  {p}", file=sys.stderr)
        return 1

    extra = sorted(sh_paths - expected)
    if extra:
        print("Pack script entries not in graph or allowlist:", file=sys.stderr)
        for p in extra:
            print(f"  {p}", file=sys.stderr)
        return 1

    print(f"verify-pack-nexo-hosting-graph-alignment: OK ({len(sh_paths)} projects)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
