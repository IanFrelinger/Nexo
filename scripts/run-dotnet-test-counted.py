#!/usr/bin/env python3
"""Run a dotnet test target and fail closed on zero or missing test execution."""

from __future__ import annotations

import argparse
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path


def discovered_count(output: str, expected_prefix: str) -> int:
    return sum(
        1
        for line in output.splitlines()
        if line.strip().startswith(expected_prefix)
    )


def executed_count(results_directory: Path) -> int:
    total = 0
    trx_files = list(results_directory.rglob("*.trx"))
    if not trx_files:
        raise ValueError("dotnet test produced no TRX result.")
    for path in trx_files:
        root = ET.parse(path).getroot()
        counters = root.find(".//{*}ResultSummary/{*}Counters")
        if counters is None:
            counters = root.find(".//{*}Counters")
        if counters is None:
            raise ValueError(f"{path} has no test counters.")
        total += int(counters.attrib.get("executed", counters.attrib.get("total", "0")))
    return total


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project", required=True)
    parser.add_argument("--expected-prefix", required=True)
    parser.add_argument("--min-tests", type=int, default=1)
    parser.add_argument("dotnet_args", nargs=argparse.REMAINDER)
    args = parser.parse_args(argv)
    if args.dotnet_args and args.dotnet_args[0] == "--":
        args.dotnet_args = args.dotnet_args[1:]
    if args.min_tests < 1:
        parser.error("--min-tests must be >= 1")
    return args


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv or sys.argv[1:])
    lowered = {arg.lower() for arg in args.dotnet_args}
    if "--logger" in lowered or "--results-directory" in lowered or "--list-tests" in lowered:
        print(
            "counted-test: logger/results/list options are coordinator-owned.",
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
        print(f"counted-test: discovery failed (exit {listed.returncode}).", file=sys.stderr)
        return listed.returncode

    expected = discovered_count(listed.stdout, args.expected_prefix)
    if expected < args.min_tests:
        print(
            f"counted-test: discovered {expected} matching tests; "
            f"required >= {args.min_tests} with prefix {args.expected_prefix!r}.",
            file=sys.stderr,
        )
        return 1

    with tempfile.TemporaryDirectory(prefix="ashlar-counted-tests-") as temp:
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
            actual = executed_count(results)
        except (OSError, ET.ParseError, ValueError) as exc:
            print(f"counted-test: invalid execution evidence: {exc}", file=sys.stderr)
            return 1

    if actual < expected:
        print(
            f"counted-test: executed {actual}, but discovery reported {expected}.",
            file=sys.stderr,
        )
        return 1
    print(f"counted-test: PASS (discovered={expected}, executed={actual})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
