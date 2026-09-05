#!/usr/bin/env python3
"""Fail closed when a TRX file executed fewer tests than the required floor.

Use this after a container run that writes TRX onto a host mount. Images may
lack Python, so the in-container invocation can stay raw; the host still
refuses a missing, unreadable, or empty result.
"""

from __future__ import annotations

import argparse
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def executed_in_trx(path: Path) -> int:
    root = ET.parse(path).getroot()
    counters = root.find(".//{*}ResultSummary/{*}Counters")
    if counters is None:
        counters = root.find(".//{*}Counters")
    if counters is None:
        raise ValueError(f"{path} has no test counters.")
    return int(counters.attrib.get("executed", counters.attrib.get("total", "0")))


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--trx", required=True)
    parser.add_argument("--min-executed", type=int, required=True)
    args = parser.parse_args(argv)
    if args.min_executed < 1:
        parser.error("--min-executed must be >= 1")
    return args


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv or sys.argv[1:])
    path = Path(args.trx)
    if not path.is_file():
        print(f"trx-min-executed: missing {path}", file=sys.stderr)
        return 1
    try:
        actual = executed_in_trx(path)
    except (OSError, ET.ParseError, ValueError) as exc:
        print(f"trx-min-executed: invalid TRX {path}: {exc}", file=sys.stderr)
        return 1
    if actual < args.min_executed:
        print(
            f"trx-min-executed: executed {actual} in {path}; "
            f"required >= {args.min_executed}.",
            file=sys.stderr,
        )
        return 1
    print(f"trx-min-executed: PASS (executed={actual}, min={args.min_executed})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
