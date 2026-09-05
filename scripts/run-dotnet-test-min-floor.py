#!/usr/bin/env python3
"""Fail closed when a sln/slnf test slice lists or executes too few tests.

Unlike run-dotnet-test-counted.py, this helper does not require every listed
identity to execute. Use it for multi-assembly filters that skip opt-in tests.
A silent empty match still fails: unique listed < --min-listed, or TRX
executed < --min-executed.
"""

from __future__ import annotations

import argparse
import importlib.util
import subprocess
import sys
import tempfile
from pathlib import Path

_COUNTED = Path(__file__).with_name("run-dotnet-test-counted.py")
_SPEC = importlib.util.spec_from_file_location("ashlar_counted_tests", _COUNTED)
if _SPEC is None or _SPEC.loader is None:
    raise SystemExit("min-floor: cannot load run-dotnet-test-counted.py")
_COUNTED_MOD = importlib.util.module_from_spec(_SPEC)
_SPEC.loader.exec_module(_COUNTED_MOD)


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project", required=True)
    parser.add_argument("--expected-prefix", required=True)
    parser.add_argument("--min-listed", type=int, required=True)
    parser.add_argument("--min-executed", type=int, default=1)
    parser.add_argument("dotnet_args", nargs=argparse.REMAINDER)
    args = parser.parse_args(argv)
    if args.dotnet_args and args.dotnet_args[0] == "--":
        args.dotnet_args = args.dotnet_args[1:]
    if args.min_listed < 1:
        parser.error("--min-listed must be >= 1")
    if args.min_executed < 1:
        parser.error("--min-executed must be >= 1")
    return args


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv or sys.argv[1:])
    lowered = {arg.lower() for arg in args.dotnet_args}
    if "--logger" in lowered or "--results-directory" in lowered or "--list-tests" in lowered:
        print(
            "min-floor: logger/results/list options are helper-owned.",
            file=sys.stderr,
        )
        return 64

    base = ["dotnet", "test", args.project, *args.dotnet_args]
    listed = subprocess.run(
        [*base, "--list-tests"],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )
    print(listed.stdout, end="")
    if listed.returncode != 0:
        print(f"min-floor: discovery failed (exit {listed.returncode}).", file=sys.stderr)
        return listed.returncode

    discovered = _COUNTED_MOD.discovered_tests(listed.stdout, args.expected_prefix)
    unique = len(set(discovered))
    if unique < args.min_listed:
        print(
            f"min-floor: discovered {unique} unique matching tests "
            f"({len(discovered)} listed); required >= {args.min_listed} with prefix "
            f"{args.expected_prefix!r}.",
            file=sys.stderr,
        )
        return 1

    with tempfile.TemporaryDirectory(prefix="ashlar-min-floor-") as temp:
        results = Path(temp)
        executed = subprocess.run(
            [
                *base,
                "--logger",
                "trx;LogFilePrefix=release-audit",
                "--results-directory",
                str(results),
            ],
            check=False,
        )
        if executed.returncode != 0:
            return executed.returncode
        try:
            actual = _COUNTED_MOD.executed_count(results)
        except (OSError, ValueError) as exc:
            print(f"min-floor: invalid execution evidence: {exc}", file=sys.stderr)
            return 1

    if actual < args.min_executed:
        print(
            f"min-floor: executed {actual}; required >= {args.min_executed}.",
            file=sys.stderr,
        )
        return 1
    print(
        f"min-floor: PASS (listed_unique={unique}, executed={actual})"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
