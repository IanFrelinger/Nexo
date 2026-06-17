#!/usr/bin/env bash
# Prints the canonical Nexo package version tracked in-repo (no v prefix).
# Source order: VERSION file at repo root, then Nexo.Hosting.Bundle metapackage PackageVersion.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [[ -f "${ROOT}/VERSION" ]]; then
  tr -d '[:space:]' < "${ROOT}/VERSION"
  exit 0
fi

sed -n 's:.*<PackageVersion>\([^<]*\)</PackageVersion>.*:\1:p' \
  "${ROOT}/src/Nexo.Hosting.Bundle/Nexo.Hosting.Bundle.csproj" | head -1
