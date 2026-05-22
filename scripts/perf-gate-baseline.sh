#!/usr/bin/env bash
# Record/compare perf metrics under .nexo/perf/baseline.json
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

REPORT_DIR=".nexo/perf"
BASELINE="$REPORT_DIR/baseline.json"
LAST="$REPORT_DIR/last-run.json"
mkdir -p "$REPORT_DIR"

python3 - <<'PY'
import json, os, glob
from datetime import datetime, timezone

report_dir = ".nexo/perf"
metrics = {"timestamp": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"), "metrics": {}}

for name in ("pipeline-throughput.json", "cold-start.json", "soak.json"):
    path = os.path.join(report_dir, name)
    if os.path.isfile(path):
        with open(path, encoding="utf-8") as fh:
            data = json.load(fh)
        key = name.replace(".json", "").replace("-", "_")
        if "totalMs" in data:
            metrics["metrics"][f"{key}_total_ms"] = data["totalMs"]
            metrics["metrics"][f"{key}_avg_ms"] = data.get("avgMsPerRun", data["totalMs"])
        if "elapsedMs" in data:
            metrics["metrics"][f"{key}_ms"] = data["elapsedMs"]
        if "runs" in data and "failures" in data:
            metrics["metrics"][f"{key}_failures"] = data["failures"]

# TRX pass counts
for trx in glob.glob(os.path.join(report_dir, "perf-*.trx")):
    try:
        with open(trx, encoding="utf-8", errors="ignore") as fh:
            content = fh.read()
        passed = content.count('outcome="Passed"')
        failed = content.count('outcome="Failed"')
        metrics["metrics"][os.path.basename(trx) + "_passed"] = passed
        metrics["metrics"][os.path.basename(trx) + "_failed"] = failed
    except OSError:
        pass

with open(os.path.join(report_dir, "last-run.json"), "w", encoding="utf-8") as fh:
    json.dump(metrics, fh, indent=2)

baseline_path = os.path.join(report_dir, "baseline.json")
update = os.environ.get("PERF_GATE_UPDATE_BASELINE", "0") == "1"
regression_pct = float(os.environ.get("PERF_GATE_REGRESSION_PCT", "25"))

if not os.path.isfile(baseline_path):
    if update or os.environ.get("PERF_GATE_INIT_BASELINE", "1") == "1":
        with open(baseline_path, "w", encoding="utf-8") as fh:
            json.dump(metrics, fh, indent=2)
        print(f"perf baseline: initialized {baseline_path}")
    else:
        print("perf baseline: no baseline.json (set PERF_GATE_UPDATE_BASELINE=1)")
    raise SystemExit(0)

with open(baseline_path, encoding="utf-8") as fh:
    baseline = json.load(fh)

failures = []
for key, value in metrics["metrics"].items():
    if not key.endswith("_ms") and not key.endswith("_total_ms") and not key.endswith("_avg_ms"):
        continue
    if key not in baseline.get("metrics", {}):
        continue
    base = baseline["metrics"][key]
    if base <= 0:
        continue
    delta_pct = ((value - base) / base) * 100.0
    if delta_pct > regression_pct:
        failures.append(f"{key}: {value} vs baseline {base} (+{delta_pct:.1f}%)")

if failures and os.environ.get("PERF_GATE_STRICT_BASELINE", "0") == "1":
    for f in failures:
        print(f"perf regression: {f}")
    raise SystemExit(1)

for f in failures:
    print(f"::warning::perf regression (advisory): {f}")

if update:
    with open(baseline_path, "w", encoding="utf-8") as fh:
        json.dump(metrics, fh, indent=2)
    print(f"perf baseline: updated {baseline_path}")

print("perf-gate-baseline: PASS")
PY
