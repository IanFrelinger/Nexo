#!/usr/bin/env python3
"""
Ensure scripts/pack-ashlar-hosting-graph.{sh,ps1} list the same Ashlar.* projects as the
transitive ProjectReference closure from src/Ashlar.Hosting, plus optional allowlisted extras.

Optional file scripts/pack-ashlar-hosting-graph.allowlist.txt: one relative csproj path per line
(lines starting with # ignored). Paths there may appear in the pack script without being
reachable from Ashlar.Hosting (rare; document why in the allowlist file).

Exit 1 with a diff if validation fails.
"""
from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path


def repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def read_allowlist(root: Path) -> set[str]:
    path = root / "scripts/pack-ashlar-hosting-graph.allowlist.txt"
    if not path.is_file():
        return set()
    out: set[str] = set()
    for line in path.read_text(encoding="utf-8").splitlines():
        s = line.split("#", 1)[0].strip()
        if not s:
            continue
        out.add(s.replace("\\", "/"))
    return out


def extract_from_sh(root: Path) -> list[str]:
    text = (root / "scripts/pack-ashlar-hosting-graph.sh").read_text(encoding="utf-8")
    paths: list[str] = []
    for line in text.splitlines():
        m = re.match(r"^pack\s+(\S+\.csproj)\s*$", line.strip())
        if m:
            paths.append(m.group(1).replace("\\", "/"))
    return sorted(paths)


def extract_from_ps1(root: Path) -> list[str]:
    text = (root / "scripts/pack-ashlar-hosting-graph.ps1").read_text(encoding="utf-8")
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
        # Analyzer-only references (OutputItemType=Analyzer / ReferenceOutputAssembly=false)
        # contribute no assembly to the runtime graph and are never packable, so they are
        # not part of "the projects the hosting graph ships". Without this, adding a Roslyn
        # analyzer to the solution demands it be added to the pack scripts, which would try
        # to pack an IsPackable=false project.
        if (it.get("OutputItemType") or "").strip().lower() == "analyzer":
            continue
        if (it.get("ReferenceOutputAssembly") or "").strip().lower() == "false":
            continue
        fp = it.get("FullPath") or it.get("Identity")
        if fp:
            out.append(Path(fp).resolve())
    return out


def transitive_ashlar_csprojs(root: Path, start: Path) -> set[str]:
    """Relative posix paths under repo for Ashlar.*.csproj reachable from Hosting."""
    rel_set: set[str] = set()
    queue: list[Path] = [start.resolve()]
    seen: set[Path] = set()
    while queue:
        p = queue.pop()
        if p in seen:
            continue
        seen.add(p)
        name = p.name
        if not name.startswith("Ashlar.") or not name.endswith(".csproj"):
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
        print("pack-ashlar-hosting-graph.sh and .ps1 disagree on which projects to pack:", file=sys.stderr)
        only_sh = sorted(sh_paths - ps1_paths)
        only_ps1 = sorted(ps1_paths - sh_paths)
        if only_sh:
            print("  only in .sh:", only_sh, file=sys.stderr)
        if only_ps1:
            print("  only in .ps1:", only_ps1, file=sys.stderr)
        return 1

    hosting = root / "src/Ashlar.Hosting/Ashlar.Hosting.csproj"
    if not hosting.is_file():
        print(f"Missing {hosting}", file=sys.stderr)
        return 1

    msbuild_set = transitive_ashlar_csprojs(root, hosting)
    allow_extra = read_allowlist(root)
    expected = msbuild_set | allow_extra

    missing_from_script = sorted(msbuild_set - sh_paths)
    if missing_from_script:
        print(
            "MSBuild graph has Ashlar.* projects not listed in pack-ashlar-hosting-graph (add to .sh/.ps1):",
            file=sys.stderr,
        )
        for p in missing_from_script:
            print(f"  {p}", file=sys.stderr)
        return 1

    only_script = sorted(sh_paths - expected)
    if only_script:
        print(
            "pack-ashlar-hosting-graph lists projects not in Ashlar.Hosting closure and not in "
            "scripts/pack-ashlar-hosting-graph.allowlist.txt:",
            file=sys.stderr,
        )
        for p in only_script:
            print(f"  {p}", file=sys.stderr)
        return 1

    print(
        "verify-pack-ashlar-hosting-graph-alignment: OK (%d packed, %d from MSBuild, %d allowlisted extra)"
        % (len(sh_paths), len(msbuild_set), len(allow_extra & sh_paths))
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
