#!/usr/bin/env bash
# Install pinned Syft/Grype release tarballs. Never curl install.sh from main.
set -euo pipefail

DEST="${1:-}"
if [ -z "$DEST" ]; then
  echo "usage: install-anchore-sbom-tools.sh <destination-dir> [--with-grype]" >&2
  exit 2
fi
WITH_GRYPE=0
if [ "${2:-}" = "--with-grype" ]; then
  WITH_GRYPE=1
fi

SYFT_VERSION="1.51.0"
SYFT_TARBALL="syft_${SYFT_VERSION}_linux_amd64.tar.gz"
SYFT_SHA256="2a2e837a2c8d59ec9af5472ee22d3b04ee463c4e44476ecf993fd1e5ab6ebc7f"

GRYPE_VERSION="0.118.0"
GRYPE_TARBALL="grype_${GRYPE_VERSION}_linux_amd64.tar.gz"
GRYPE_SHA256="1d444c5e7360471815f7158f71935fcecc68a3c417d85c7344f770854300bba2"

mkdir -p "$DEST"
WORKDIR="$(mktemp -d)"
trap 'rm -rf "$WORKDIR"' EXIT

fetch_and_verify() {
  local url="$1"
  local archive="$2"
  local expected="$3"
  curl -fsSL -o "$WORKDIR/$archive" "$url"
  local actual
  actual="$(sha256sum "$WORKDIR/$archive" | awk '{print $1}')"
  if [ "$actual" != "$expected" ]; then
    echo "checksum mismatch for $archive: got $actual expected $expected" >&2
    exit 1
  fi
  tar -xzf "$WORKDIR/$archive" -C "$DEST"
}

fetch_and_verify \
  "https://github.com/anchore/syft/releases/download/v${SYFT_VERSION}/${SYFT_TARBALL}" \
  "$SYFT_TARBALL" \
  "$SYFT_SHA256"

if [ ! -x "$DEST/syft" ]; then
  echo "syft binary missing after extract" >&2
  exit 1
fi

if [ "$WITH_GRYPE" -eq 1 ]; then
  fetch_and_verify \
    "https://github.com/anchore/grype/releases/download/v${GRYPE_VERSION}/${GRYPE_TARBALL}" \
    "$GRYPE_TARBALL" \
    "$GRYPE_SHA256"
  if [ ! -x "$DEST/grype" ]; then
    echo "grype binary missing after extract" >&2
    exit 1
  fi
fi

echo "anchore tools installed in $DEST (syft ${SYFT_VERSION}$([ "$WITH_GRYPE" -eq 1 ] && printf ', grype %s' "$GRYPE_VERSION"))"
