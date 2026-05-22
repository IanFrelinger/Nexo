#!/usr/bin/env bash
# Tier D kernel gate: pack graph alignment + local NuGet consumer sample (isolated cache).
# See docs/NuGetConsumerVerify.md and docs/production-readiness/KernelHardeningPlan-v1.md
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

VERSION="${NEXO_SDK_PACKAGE_VERSION:-0.0.0-kernel-gate-local}"
export NEXO_SDK_PACKAGE_VERSION="${VERSION}"

echo "== Tier D: runtime graph builds without application hosts =="
dotnet build Nexo.Runtime.sln -v minimal

echo "== Tier D: pack list vs Nexo.Hosting MSBuild closure =="
python3 scripts/verify-pack-nexo-hosting-graph-alignment.py

echo "== Tier D: pack local feed + StableSdkHostSample consumer (version ${VERSION}) =="
bash scripts/verify-stable-sdk-host-sample-packages.sh

echo ""
echo "kernel-gate-tier-d: PASS"
