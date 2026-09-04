#!/usr/bin/env bash
# C6: docs that name a published package version must key off ci/published-version,
# never the repo VERSION file (which may already have been bumped for an unpublished release).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISHED="$(tr -d '[:space:]' < "${ROOT}/ci/published-version")"
VERSION="$(tr -d '[:space:]' < "${ROOT}/VERSION")"

if [[ -z "${PUBLISHED}" ]]; then
  echo "C6: ci/published-version is empty" >&2
  exit 1
fi

fail=0

# "from VERSION" (with or without backticks) is the exact lie C6 forbids:
# it treats the unpublished repo pin as the public feed version.
while IFS= read -r -d '' file; do
  if grep -nE 'from[[:space:]]+`?VERSION`?' "${file}" | grep -viE 'never|not |must not|do not|forbid' >/dev/null; then
    echo "C6: ${file} cites VERSION as a published pin; key off ci/published-version (${PUBLISHED})" >&2
    grep -nE 'from[[:space:]]+`?VERSION`?' "${file}" >&2 || true
    fail=1
  fi
done < <(find "${ROOT}/docs" "${ROOT}/consumer-template" -type f \( -name '*.md' -o -name '*.txt' \) -print0)

if [[ "${VERSION}" != "${PUBLISHED}" ]]; then
  # Repo has been bumped ahead of the feed. Docs must not advertise VERSION as shipped.
  while IFS= read -r -d '' file; do
    if grep -nE "published[[:space:]]+(on nuget|to nuget|version).*${VERSION}|nuget.org.*${VERSION}" "${file}" >/dev/null; then
      echo "C6: ${file} advertises unpublished VERSION ${VERSION} as if it were on the feed (published is ${PUBLISHED})" >&2
      fail=1
    fi
  done < <(find "${ROOT}/docs" "${ROOT}/consumer-template" -type f -name '*.md' -print0)
fi

if [[ "${fail}" -ne 0 ]]; then
  exit 1
fi

echo "C6: docs published-version lint ok (published=${PUBLISHED}, repo VERSION=${VERSION})"
