#!/usr/bin/env bash
# Verify external product shape against a published staging feed (post-push).
#
# Usage:
#   NUGET_STAGING_READ_TOKEN=*** bash scripts/verify-staging.sh 0.1.0
#   make verify-staging VERSION=0.1.0
#
# Reads feed URL from gh variable NUGET_STAGING_FEED_URL; token from NUGET_STAGING_READ_TOKEN.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${VERSION:-${1:-}}"

if [[ -z "${VERSION}" ]]; then
  echo "usage: verify-staging.sh <semver>   or   make verify-staging VERSION=x.y.z" >&2
  exit 1
fi

VERSION="${VERSION#v}"

PUBLISHED_SCRIPT="${ROOT}/scripts/verify-external-product-shape-published.sh"
if [[ ! -x "${PUBLISHED_SCRIPT}" ]]; then
  echo "ABORT: missing executable ${PUBLISHED_SCRIPT}" >&2
  exit 1
fi

if ! command -v gh >/dev/null 2>&1; then
  echo "ABORT: gh is required to read NUGET_STAGING_FEED_URL." >&2
  exit 1
fi

if ! gh auth status >/dev/null 2>&1; then
  echo "ABORT: gh is not authenticated (gh auth login)." >&2
  exit 1
fi

FEED_URL="$(gh variable get NUGET_STAGING_FEED_URL 2>/dev/null | tr -d '[:space:]' || true)"
if [[ -z "${FEED_URL}" ]]; then
  echo "ABORT: NUGET_STAGING_FEED_URL is not set." >&2
  exit 1
fi

if [[ -z "${NUGET_STAGING_READ_TOKEN:-}" ]]; then
  echo "ABORT: NUGET_STAGING_READ_TOKEN is not set in the environment (read PAT for the staging feed)." >&2
  exit 1
fi

# GitHub Packages / private feeds: username + token (never logged).
export NEXO_NUGET_USERNAME="${NEXO_NUGET_USERNAME:-token}"
export NEXO_NUGET_PASSWORD="${NEXO_STAGING_READ_TOKEN}"

echo "verify-staging: version=${VERSION} feed=${FEED_URL}"
bash "${PUBLISHED_SCRIPT}" "${VERSION}" "${FEED_URL}"
