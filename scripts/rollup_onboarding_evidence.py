#!/usr/bin/env python3
"""Emit buyer-facing aggregate metrics from append-only onboarding JSONL."""
import argparse
import json
import statistics
from pathlib import Path


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("input", type=Path)
    p.add_argument("--output", type=Path)
    a = p.parse_args()
    rows = [json.loads(line) for line in a.input.read_text(encoding="utf-8").splitlines() if line.strip()]
    valid = [r for r in rows if r.get("record_type") == "onboarding_run"]
    passed = [r for r in valid if r["outcome"] == "PASS"]
    labeled = [r for r in valid if r.get("triage", {}).get("correct") is not None]
    result = {
        "projects": len(valid),
        "oracle_pass_rate": (sum(r["output"]["oracle"]["pass"] for r in valid) / len(valid)) if valid else None,
        "median_time_to_valid_rows_s": statistics.median([r["timing"]["time_to_valid_rows_s"] for r in passed]) if passed else None,
        "triage_accuracy": (sum(r["triage"]["correct"] for r in labeled) / len(labeled)) if labeled else None,
        "labeled_runs": len(labeled),
        "model_assist_rate": (sum(r["adapter"]["source"] == "model_assisted" for r in valid) / len(valid)) if valid else None,
    }
    text = json.dumps(result, indent=2, sort_keys=True)
    print(text)
    if a.output:
        a.output.write_text(text + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
