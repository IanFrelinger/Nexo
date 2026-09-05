#!/usr/bin/env python3
"""Assert Production Readiness Gate v1 fail-closed pipeline CLI outcomes.

Unconfigured ``pipeline run`` and LiteDB resume must stay Failed. The default
placeholder adapter performs no work; a green Completed result is fabricated.

Commands:
  fail-closed <log> <label>
  resume <source-log> <target-log>
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

ADAPTER_ERROR = "No deterministic pipeline adapter is configured"
MISSING_PRIOR = "no prior run was found"


def parse_final_json(path: str) -> dict:
    text = Path(path).read_text(encoding="utf-8")
    json_lines = [
        line.strip()
        for line in text.splitlines()
        if line.strip().startswith("{") and line.strip().endswith("}")
    ]
    if not json_lines:
        raise SystemExit(f"No JSON payload found in {path}")
    return json.loads(json_lines[-1])


def assert_fail_closed(payload: dict, label: str) -> None:
    if payload.get("ok"):
        raise SystemExit(f"{label}: unconfigured pipeline run must not report ok=true")
    data = payload.get("data") or {}
    if data.get("state") != "Failed":
        raise SystemExit(f"{label}: expected state Failed, got {data.get('state')}")
    ingest = next((s for s in data.get("stages", []) if s.get("stageId") == "ingest"), None)
    if ingest is None:
        raise SystemExit(f"{label}: missing ingest stage")
    error = ingest.get("error") or ""
    if ADAPTER_ERROR not in error:
        raise SystemExit(
            f"{label}: ingest did not fail closed on the placeholder adapter: {error!r}"
        )


def assert_resume(source: dict, target: dict) -> None:
    if source.get("data", {}).get("state") != "Failed":
        raise SystemExit("Source run is expected to fail for durable resume check")
    if target.get("ok"):
        raise SystemExit("Resumed target must not report ok=true without a configured adapter")
    if target.get("data", {}).get("state") != "Failed":
        raise SystemExit(
            f"Resumed target must stay Failed, got {target.get('data', {}).get('state')}"
        )
    if MISSING_PRIOR in json.dumps(target):
        raise SystemExit("Resume did not find the persisted source run")


def main(argv: list[str]) -> int:
    usage = (
        "usage: assert-pipeline-fail-closed.py fail-closed <log> <label> "
        "| resume <source-log> <target-log>"
    )
    if len(argv) < 2:
        raise SystemExit(usage)
    cmd = argv[1]
    if cmd == "fail-closed":
        if len(argv) != 4:
            raise SystemExit("usage: assert-pipeline-fail-closed.py fail-closed <log> <label>")
        assert_fail_closed(parse_final_json(argv[2]), argv[3])
        print(f"{argv[3]}: fail-closed PASS")
        return 0
    if cmd == "resume":
        if len(argv) != 4:
            raise SystemExit(
                "usage: assert-pipeline-fail-closed.py resume <source-log> <target-log>"
            )
        assert_resume(parse_final_json(argv[2]), parse_final_json(argv[3]))
        print("durable resume (fail-closed): PASS")
        return 0
    raise SystemExit(f"unknown command: {cmd}\n{usage}")


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
