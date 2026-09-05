#!/usr/bin/env python3
"""Fail closed when `ashlar test local --format-json` reports zero discovered tests."""

from __future__ import annotations

import json
import re
import sys


def parse_total_tests(text: str) -> int | None:
    for line in text.splitlines():
        stripped = line.strip()
        if stripped.startswith("{") and "TotalTests" in stripped:
            try:
                return int(json.loads(stripped)["TotalTests"])
            except (json.JSONDecodeError, KeyError, TypeError, ValueError):
                continue
    match = re.search(r'"TotalTests"\s*:\s*(\d+)', text)
    return int(match.group(1)) if match else None


def main(argv: list[str]) -> int:
    path = argv[1] if len(argv) > 1 else None
    text = sys.stdin.read() if path in (None, "-") else open(path, encoding="utf-8").read()
    total = parse_total_tests(text)
    if total is None or total < 1:
        print(
            "error: ashlar test local matched 0 tests (or omitted TotalTests); "
            "refusing skip-and-pass",
            file=sys.stderr,
        )
        return 1
    print(f"workflow-regression: test local TotalTests={total}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
