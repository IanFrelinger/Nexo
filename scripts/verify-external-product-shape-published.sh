#!/usr/bin/env bash
# External product shape verification against an already-published Ashlar.* feed (staging or private).
#
# Usage:
#   bash scripts/verify-external-product-shape-published.sh 1.2.3
#   bash scripts/verify-external-product-shape-published.sh 1.2.3 https://nuget.pkg.github.com/Org/index.json
#
# Auth (optional, for private feeds):
#   ASHLAR_NUGET_USERNAME + ASHLAR_NUGET_PASSWORD
#   or NUGET_STAGING_READ_TOKEN (sets password; username defaults to 'token')
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${1:?usage: verify-external-product-shape-published.sh <PackageVersion> [feed-url]}"
FEED_URL="${2:-}"

if [[ -z "${FEED_URL}" ]]; then
  if ! command -v gh >/dev/null 2>&1 || ! gh auth status >/dev/null 2>&1; then
    echo "usage: verify-external-product-shape-published.sh <PackageVersion> <feed-url>" >&2
    echo "       or configure gh auth + NUGET_STAGING_FEED_URL variable." >&2
    exit 1
  fi
  FEED_URL="$(gh variable get NUGET_STAGING_FEED_URL 2>/dev/null | tr -d '[:space:]' || true)"
fi

if [[ -z "${FEED_URL}" ]]; then
  echo "ABORT: feed URL required (argument or NUGET_STAGING_FEED_URL)." >&2
  exit 1
fi

if [[ -n "${NUGET_STAGING_READ_TOKEN:-}" && -z "${ASHLAR_NUGET_PASSWORD:-}" ]]; then
  export ASHLAR_NUGET_USERNAME="${ASHLAR_NUGET_USERNAME:-token}"
  export ASHLAR_NUGET_PASSWORD="${NUGET_STAGING_READ_TOKEN}"
fi

export ASHLAR_EXTERNAL_PRODUCT_VERIFY_VERSION="${VERSION#v}"
export ASHLAR_EXTERNAL_PRODUCT_PACKAGE_FEED="${FEED_URL}"
export ASHLAR_VERIFY_SOURCE_KEY="${ASHLAR_VERIFY_SOURCE_KEY:-staging-published}"

bash "${ROOT}/scripts/verify-external-product-shape.sh"
