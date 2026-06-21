#!/usr/bin/env bash
# Thin entrypoint for brick certification gate (logic lives in src via Nexo.CertifyBrick).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BRICK_DIR="${1:?Usage: certify-brick-gate.sh <brickProjectDir> [witnessSpec.json] [recordOutputPath]}"
WITNESS="${2:-${ROOT}/spikes/portability/witness/error-summary-extractor.witness.json}"
RECORD="${3:-}"

if [[ -n "${RECORD}" ]]; then
  dotnet run --project "${ROOT}/tools/Nexo.CertifyBrick/Nexo.CertifyBrick.csproj" -c Release -- "${BRICK_DIR}" "${WITNESS}" "${RECORD}"
else
  dotnet run --project "${ROOT}/tools/Nexo.CertifyBrick/Nexo.CertifyBrick.csproj" -c Release -- "${BRICK_DIR}" "${WITNESS}"
fi
