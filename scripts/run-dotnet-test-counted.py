#!/usr/bin/env python3
"""Run a dotnet test target and fail closed on zero or missing test execution."""

from __future__ import annotations

import argparse
import collections
import re
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path


def _identity_starts(line: str, expected_prefix: str) -> list[int]:
    """Offsets where a new identity begins.

    Multi-TFM discovery writes both frameworks to one stream, so two identities can land
    on a single line; each one has to stay whole, because fusing them invents an identity
    that can never execute AND drops one from the floor, letting a genuinely missing test
    pass underneath it. A theory row's arguments can also contain the prefix
    (``Method(testType: typeof(Ashlar.Tests.CLI.Some.Type))``), so only a match outside
    the argument list starts an identity -- splitting inside it would invent two.
    """
    starts: list[int] = []
    depth = 0
    for index, character in enumerate(line):
        if character in "([":
            depth += 1
            continue
        if character in ")]":
            depth = max(0, depth - 1)
            continue
        if depth or not line.startswith(expected_prefix, index):
            continue
        if index == 0 or line[index - 1].isspace():
            starts.append(index)
    return starts


def discovered_tests(output: str, expected_prefix: str) -> list[str]:
    identities: list[str] = []
    for line in output.splitlines():
        stripped = line.strip()
        if not stripped.startswith(expected_prefix):
            continue
        starts = _identity_starts(stripped, expected_prefix)
        for index, start in enumerate(starts):
            end = starts[index + 1] if index + 1 < len(starts) else len(stripped)
            identity = stripped[start:end].strip()
            if identity:
                identities.append(identity)
    return identities


def executed_evidence(
    results_directory: Path,
) -> tuple[int, collections.Counter[str], collections.Counter[str]]:
    total = 0
    all_outcomes: collections.Counter[str] = collections.Counter()
    passed: collections.Counter[str] = collections.Counter()
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
        for result in root.findall(".//{*}UnitTestResult"):
            name = result.attrib.get("testName")
            outcome = result.attrib.get("outcome")
            if not name or not outcome:
                raise ValueError(f"{path} has an incomplete UnitTestResult.")
            all_outcomes[name] += 1
            if outcome.lower() == "passed":
                passed[name] += 1
    if sum(all_outcomes.values()) != total:
        raise ValueError(
            f"TRX counters report {total} executions, but "
            f"{sum(all_outcomes.values())} result identities were recorded."
        )
    return total, all_outcomes, passed


def executed_count(results_directory: Path) -> int:
    return executed_evidence(results_directory)[0]


def _covers(discovered_name: str, executed_names: collections.Counter[str]) -> bool:
    """Exact TRX name, or xUnit theory rows listed as Method(data)."""
    if executed_names[discovered_name]:
        return True
    prefix = discovered_name + "("
    return any(name.startswith(prefix) for name in executed_names)


def identity_problems(
    discovered: list[str],
    all_outcomes: collections.Counter[str],
    passed: collections.Counter[str],
) -> list[str]:
    problems: list[str] = []
    missing = [name for name in discovered if not _covers(name, all_outcomes)]
    not_passed = [name for name in discovered if not _covers(name, passed)]
    if missing:
        problems.append(
            "discovered identities did not execute: " + ", ".join(sorted(set(missing)))
        )
    if not_passed:
        problems.append(
            "mandatory identities were not passed: " + ", ".join(sorted(set(not_passed)))
        )
    return problems


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

    discovered = discovered_tests(listed.stdout, args.expected_prefix)
    expected = len(discovered)
    unique = len(set(discovered))
    if unique < args.min_tests:
        print(
            f"counted-test: discovered {unique} unique matching tests "
            f"({expected} listed); required >= {args.min_tests} with prefix "
            f"{args.expected_prefix!r}.",
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
            actual, all_outcomes, passed = executed_evidence(results)
        except (OSError, ET.ParseError, ValueError) as exc:
            print(f"counted-test: invalid execution evidence: {exc}", file=sys.stderr)
            return 1

    if actual < expected:
        print(
            f"counted-test: executed {actual}, but discovery reported {expected}.",
            file=sys.stderr,
        )
        return 1
    problems = identity_problems(discovered, all_outcomes, passed)
    if problems:
        print("counted-test: " + "; ".join(problems), file=sys.stderr)
        return 1
    print(f"counted-test: PASS (discovered={expected}, executed={actual})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
