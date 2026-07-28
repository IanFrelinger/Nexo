#!/usr/bin/env bash
# Download Docker Compose v2 plugin binary into docker/dep-extract-gui/vendor/
# so agent image builds can COPY it without GitHub egress.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VER="${COMPOSE_VERSION:-v2.36.2}"
ARCH="${COMPOSE_ARCH:-x86_64}"
OUT_DIR="$ROOT/docker/dep-extract-gui/vendor"
OUT="$OUT_DIR/docker-compose-linux-${ARCH}"
URL="https://github.com/docker/compose/releases/download/${VER}/docker-compose-linux-${ARCH}"

mkdir -p "$OUT_DIR"
if [ -f "$OUT" ] && [ "${FORCE_VENDOR:-0}" != "1" ]; then
  echo "already vendored: $OUT"
  ls -lh "$OUT"
  exit 0
fi

echo "== download $URL =="
curl -fsSL "$URL" -o "$OUT"
chmod +x "$OUT"
echo "vendored: $OUT ($(wc -c < "$OUT") bytes)"
echo "$VER" > "$OUT_DIR/COMPOSE_VERSION.txt"
