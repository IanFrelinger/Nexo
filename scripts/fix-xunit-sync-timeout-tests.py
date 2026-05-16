#!/usr/bin/env python3
"""Convert sync [Fact|Theory(Timeout)] tests to async Task + await Task.CompletedTask (xUnit 2.9+)."""

from __future__ import annotations

import re
import sys
from pathlib import Path

# After [Fact(Timeout)] / [Theory(Timeout)], the next method may be separated by
# other attributes (e.g. [InlineData]).
_METHOD_START = re.compile(
    r"^(\s*)public\s+(async\s+)?(void|Task)\s+(\w+)\s*\("
)
_ATTR_WITH_TIMEOUT = re.compile(r"^\s*\[(Fact|Theory)\(Timeout")


def _next_nonempty_line_index(lines: list[str], start: int) -> int | None:
    j = start
    while j < len(lines):
        if lines[j].strip():
            return j
        j += 1
    return None


def _find_open_brace_line(lines: list[str], sig_start: int) -> tuple[int, bool]:
    """Return (line_index_of_brace, brace_only_line). Walks multi-line signatures."""
    idx = sig_start
    depth = lines[idx].count("(") - lines[idx].count(")")
    while idx < len(lines) and depth > 0:
        idx += 1
        if idx >= len(lines):
            break
        depth += lines[idx].count("(") - lines[idx].count(")")
    scan_from = idx
    for k in range(scan_from, min(scan_from + 10, len(lines))):
        ln = lines[k]
        if "{" not in ln:
            continue
        stripped = ln.strip()
        if stripped == "{":
            return k, True
        if "{" in ln:
            return k, stripped.endswith("{") and ln.rstrip().endswith("{")
    return sig_start, False


def _insert_after_open_brace(lines: list[str], brace_idx: int, brace_only: bool) -> None:
    ln = lines[brace_idx]
    indent_match = re.match(r"^(\s*)", ln)
    base_indent = indent_match.group(1) if indent_match else ""

    if brace_only and ln.strip() == "{":
        inner = base_indent + "    "
        insert_at = brace_idx + 1
        ni = _next_nonempty_line_index(lines, insert_at)
        if ni is not None:
            nxs = lines[ni].lstrip()
            if nxs.startswith(("await ", "return ", "throw ")):
                return
        lines.insert(insert_at, f"{inner}await Task.CompletedTask;")
        return

    # Brace on same line as signature or closing paren
    pos = ln.find("{")
    if pos < 0:
        return
    after = ln[pos + 1 :]
    inner = base_indent + "    "
    if after.strip():
        rest_stripped = after.lstrip()
        if rest_stripped.startswith(("await ", "return ", "throw ")):
            return
        lines[brace_idx] = ln[: pos + 1]
        lines.insert(brace_idx + 1, f"{inner}await Task.CompletedTask;")
        if after.strip():
            lines.insert(brace_idx + 2, after)
        return

    inner_indent = base_indent + "    "
    insert_at = brace_idx + 1
    ni = _next_nonempty_line_index(lines, insert_at)
    if ni is not None:
        nxs = lines[ni].lstrip()
        if nxs.startswith(("await ", "return ", "throw ")):
            return
    lines.insert(insert_at, f"{inner_indent}await Task.CompletedTask;")


def _find_method_after_timeout(lines: list[str], timeout_attr_idx: int) -> int | None:
    """First line that starts a public instance method (void or Task). Skips attributes."""
    j = timeout_attr_idx + 1
    while j < len(lines):
        raw = lines[j]
        stripped = raw.strip()
        if stripped.startswith("public ") and "(" in raw:
            return j
        # Skip attributes and trivia (comments only — careful: block comments rare)
        if stripped.startswith("//") or stripped.startswith("#"):
            j += 1
            continue
        if stripped.startswith("["):
            j += 1
            continue
        if not stripped:
            j += 1
            continue
        # Hit something else (e.g. closing brace of previous method) — stop
        break
    return None


def process_file(text: str) -> tuple[str, int]:
    lines = text.split("\n")
    converted = 0
    i = 0
    while i < len(lines):
        line = lines[i]
        if not _ATTR_WITH_TIMEOUT.match(line):
            i += 1
            continue

        attr_idx = i
        meth_idx = _find_method_after_timeout(lines, attr_idx)
        if meth_idx is None:
            i += 1
            continue

        sig_line = lines[meth_idx]
        m = _METHOD_START.match(sig_line)
        if not m:
            i += 1
            continue

        if m.group(2):  # async
            i += 1
            continue

        ret = m.group(3)
        if ret != "void":
            i += 1
            continue

        lines[meth_idx] = sig_line.replace("public void ", "public async Task ", 1)
        converted += 1

        brace_idx, brace_only = _find_open_brace_line(lines, meth_idx)
        _insert_after_open_brace(lines, brace_idx, brace_only)

        i = attr_idx + 1

    return "\n".join(lines), converted


def main() -> int:
    root = Path(__file__).resolve().parents[1] / "src"
    if not root.is_dir():
        print(f"Missing {root}", file=sys.stderr)
        return 1
    total_files = 0
    total_methods = 0
    for path in sorted(root.rglob("*.cs")):
        raw = path.read_text(encoding="utf-8")
        if "public void" not in raw:
            continue
        if "[Fact(Timeout" not in raw and "[Theory(Timeout" not in raw:
            continue
        fixed, n = process_file(raw)
        if n == 0:
            continue
        path.write_text(fixed, encoding="utf-8")
        total_files += 1
        total_methods += n
        print(f"{path.relative_to(root.parents[1])}: {n} method(s)")
    print(f"Files updated: {total_files}; methods converted: {total_methods}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
