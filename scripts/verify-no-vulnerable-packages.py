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


def validate_report(report: Any, expected_projects: int) -> list[str]:
    problems: list[str] = []
    if not isinstance(report, dict):
        return ["report root is not an object"]
    if report.get("version") != 1:
        problems.append(f"unsupported report version: {report.get('version')!r}")
    parameters = report.get("parameters")
    if not isinstance(parameters, str) or "--vulnerable" not in parameters:
        problems.append("report does not describe a vulnerability scan")
    sources = report.get("sources")
    if not isinstance(sources, list) or not sources:
        problems.append("report has no advisory sources")
    nuget_problems = report.get("problems")
    if nuget_problems:
        problems.append(f"NuGet reported problems: {nuget_problems!r}")
    projects = report.get("projects")
    if not isinstance(projects, list):
        problems.append("report projects is not an array")
        return problems
    if len(projects) != expected_projects:
        problems.append(
            f"report covers {len(projects)} projects; expected {expected_projects}"
        )
    if any(not isinstance(project, dict) or not project.get("path") for project in projects):
        problems.append("one or more project records has no path")
    return problems


def expected_project_count(target: str) -> int:
    if target.lower().endswith((".sln", ".slnx")):
        run = subprocess.run(
            ["dotnet", "sln", target, "list"],
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
        )
        if run.returncode != 0:
            raise ValueError(
                f"could not enumerate {target} (exit {run.returncode}): {run.stdout}"
            )
        count = sum(
            1
            for line in run.stdout.splitlines()
            if line.strip().lower().endswith(".csproj")
        )
        if count < 1:
            raise ValueError(f"{target} contains no projects")
        return count
    return 1


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
        "--output-version",
        "1",
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

    try:
        expected_projects = expected_project_count(args.target)
    except ValueError as exc:
        print(f"vulnerability-scan: invalid target coverage: {exc}", file=sys.stderr)
        return 2
    report_problems = validate_report(report, expected_projects)
    if report_problems:
        print(
            "vulnerability-scan: unusable evidence: " + "; ".join(report_problems),
            file=sys.stderr,
        )
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
