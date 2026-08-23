#!/usr/bin/env bash
# Prints the canonical Ashlar package version tracked in-repo (no v prefix).
# Source order: VERSION file at repo root, then Ashlar.Hosting.Bundle metapackage PackageVersion.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [[ -f "${ROOT}/VERSION" ]]; then
  tr -d '[:space:]' < "${ROOT}/VERSION"
  exit 0
fi

sed -n 's:.*<PackageVersion>\([^<]*\)</PackageVersion>.*:\1:p' \
  "${ROOT}/src/Ashlar.Hosting.Bundle/Ashlar.Hosting.Bundle.csproj" | head -1
