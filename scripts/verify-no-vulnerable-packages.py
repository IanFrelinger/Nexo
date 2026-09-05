#!/usr/bin/env python3
"""Fail when dotnet reports any vulnerable package in the requested target."""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from typing import Any


def vulnerability_records(value: Any) -> list[dict[str, Any]]:
    records: list[dict[str, Any]] = []
    if isinstance(value, dict):
        vulnerabilities = value.get("vulnerabilities")
        if isinstance(vulnerabilities, list) and vulnerabilities:
            records.append(value)
        for nested in value.values():
            records.extend(vulnerability_records(nested))
    elif isinstance(value, list):
        for nested in value:
            records.extend(vulnerability_records(nested))
    return records


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("target", help="Solution or project to scan.")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv or sys.argv[1:])
    command = [
        "dotnet",
        "list",
        args.target,
        "package",
        "--vulnerable",
        "--include-transitive",
        "--format",
        "json",
    ]
    run = subprocess.run(
        command,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )
    print(run.stdout, end="")
    if run.returncode != 0:
        print(f"vulnerability-scan: dotnet failed (exit {run.returncode}).", file=sys.stderr)
        return run.returncode

    json_start = run.stdout.find("{")
    if json_start < 0:
        print("vulnerability-scan: dotnet produced no JSON evidence.", file=sys.stderr)
        return 2
    try:
        report = json.loads(run.stdout[json_start:])
    except json.JSONDecodeError as exc:
        print(f"vulnerability-scan: malformed JSON evidence: {exc}", file=sys.stderr)
        return 2

    vulnerable = vulnerability_records(report)
    if vulnerable:
        package_names = sorted(
            {
                str(record.get("id") or record.get("name") or "<unknown>")
                for record in vulnerable
            }
        )
        print(
            "vulnerability-scan: BLOCKED; vulnerable packages="
            + ",".join(package_names),
            file=sys.stderr,
        )
        return 1

    print("vulnerability-scan: PASS; no vulnerable packages reported.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
