#!/usr/bin/env bash
# Ratchet line-coverage floors for the applications layer.
# Philosophy per scripts/ci/kernel-coverage-gate.sh: each floor sits just
# under the measured baseline (2026-08-26, _handoff/readiness/COVERAGE-AUDIT.md)
# and is a RATCHET — it may be raised as tests earn it, never lowered. A floor
# that moves down to green a build measures nothing.
# Baselines (line): Contracts 92.1 · Runtime 92.5 · Certification.Physical
# 82.1 · Multiplayer 81.8 · ARKit 61.2 · VisionPro 58.3 · XREAL 31.3 ·
# Provenance.Graph 49.9 (unit slice only — the Neo4j integration slice, tier D,
# needs a Docker daemon; re-measure and raise that floor when it lands).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
COV_DIR="${TMPDIR:-/tmp}/applications-coverage-$$"
mkdir -p "$COV_DIR"
trap 'rm -rf "$COV_DIR"' EXIT

echo "== coverage run: Ashlar.Applications.Tests =="
dotnet test applications/Ashlar.Applications.Tests/Ashlar.Applications.Tests.csproj \
  --configuration Release \
  /p:CollectCoverage=true \
  /p:CoverletOutput="$COV_DIR/appstests." \
  /p:CoverletOutputFormat=cobertura \
  -v minimal

echo "== coverage run: Ashlar.Provenance.Graph.Tests (unit slice) =="
dotnet test applications/Ashlar.Provenance.Graph.Tests/Ashlar.Provenance.Graph.Tests.csproj \
  --configuration Release \
  --filter "Category!=Integration" \
  /p:CollectCoverage=true \
  /p:CoverletOutput="$COV_DIR/prov." \
  /p:CoverletOutputFormat=cobertura \
  -v minimal

echo "== floors =="
python3 - "$COV_DIR" <<'PY'
import sys, glob, xml.etree.ElementTree as ET

cov_dir = sys.argv[1]
# Assembly -> line-coverage floor (%). RATCHET: raise only.
FLOORS = {
    "Ashlar.Spatial.Contracts": 90.0,
    "Ashlar.Spatial.Runtime": 90.0,
    "Ashlar.Certification.Physical": 80.0,
    "Ashlar.Spatial.Multiplayer": 79.0,
    "Ashlar.Spatial.Platform.ARKit": 59.0,
    "Ashlar.Spatial.Platform.VisionPro": 56.0,
    "Ashlar.Spatial.Platform.XREAL": 29.0,
    "Ashlar.Provenance.Graph": 48.0,
}
# An assembly referenced by both suites (Certification.Physical) is judged by
# the suite that covers it best — the floor guards the owning suite's number.
best = {}
files = sorted(glob.glob(cov_dir + "/*.xml"))
if not files:
    print("applications-coverage-gate: FAIL — no cobertura output found")
    sys.exit(1)
for f in files:
    for pkg in ET.parse(f).getroot().iter("package"):
        name = pkg.get("name")
        if name in FLOORS:
            lr = float(pkg.get("line-rate")) * 100
            best[name] = max(best.get(name, 0.0), lr)

failed = []
for name, floor in sorted(FLOORS.items()):
    lr = best.get(name)
    if lr is None:
        failed.append(f"{name}: not measured (floor {floor}%)")
        print(f"  ?? {name}: NOT MEASURED (floor {floor}%)")
        continue
    ok = lr >= floor
    print(f"  {'OK ' if ok else 'LOW'} {name}: {lr:.1f}% (floor {floor}%)")
    if not ok:
        failed.append(f"{name}: {lr:.1f}% < floor {floor}%")

if failed:
    print("applications-coverage-gate: FAIL")
    for msg in failed:
        print("  " + msg)
    sys.exit(1)
print("applications-coverage-gate: PASS")
PY
