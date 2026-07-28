#!/usr/bin/env python3
"""Fail CI when any deployment/onboarding JSONL evidence record is malformed."""
import argparse
import json
import sys
from pathlib import Path

from jsonschema import Draft202012Validator

FILES = {"deployment_evidence": "deployment_evidence.json", "onboarding_run": "onboarding_run.json"}


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--schema-dir", type=Path, default=Path("schemas"))
    p.add_argument("files", nargs="+", type=Path)
    a = p.parse_args()
    validators = {kind: Draft202012Validator(json.loads((a.schema_dir / name).read_text()))
                  for kind, name in FILES.items()}
    errors, seen = [], 0
    for path in a.files:
        for line_no, raw in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if not raw.strip():
                continue
            seen += 1
            try:
                row = json.loads(raw)
            except json.JSONDecodeError as exc:
                errors.append(f"{path}:{line_no}: invalid JSON: {exc.msg}")
                continue
            validator = validators.get(row.get("record_type"))
            if not validator:
                errors.append(f"{path}:{line_no}: unknown or missing record_type")
                continue
            for err in validator.iter_errors(row):
                loc = "/".join(map(str, err.path)) or "<root>"
                errors.append(f"{path}:{line_no}: [{loc}] {err.message}")
    if errors:
        print("FAIL — evidence validation")
        print("\n".join(errors))
        return 1
    print(f"PASS — {seen} evidence records validated")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
