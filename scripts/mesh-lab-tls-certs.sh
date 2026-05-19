#!/usr/bin/env bash
# Generate self-signed TLS cert for mesh-lab Caddy proxy (gitignored output dir).

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT_DIR="${MESH_LAB_TLS_CERT_DIR:-${ROOT}/.mesh-lab-tls-certs}"

mkdir -p "$OUT_DIR"
CERT="${OUT_DIR}/cert.pem"
KEY="${OUT_DIR}/key.pem"

if [[ -f "$CERT" && -f "$KEY" && "${MESH_LAB_TLS_FORCE_REGEN:-}" != "1" ]]; then
  echo "$OUT_DIR"
  exit 0
fi

openssl req -x509 -newkey rsa:2048 \
  -keyout "$KEY" \
  -out "$CERT" \
  -days 2 \
  -nodes \
  -subj "/CN=mesh-lab-local" \
  -addext "subjectAltName=DNS:localhost,DNS:mesh-tls-proxy,IP:127.0.0.1" \
  2>/dev/null

chmod 600 "$KEY" 2>/dev/null || true
echo "$OUT_DIR"
