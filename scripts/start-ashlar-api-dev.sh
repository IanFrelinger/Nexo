#!/usr/bin/env bash
# Docker Ollama + Ashlar.API on the host (Linux, macOS, WSL; Docker Desktop / Engine).
# Usage:
#   bash scripts/start-ashlar-api-dev.sh
#   bash scripts/start-ashlar-api-dev.sh --pull
#   bash scripts/start-ashlar-api-dev.sh --model llama3.2 --api-port 8080
#   bash scripts/start-ashlar-api-dev.sh --skip-ollama   # Ollama already running
#   bash scripts/start-ashlar-api-dev.sh --listen-lan   # phone / other devices on same Wi-Fi
set -euo pipefail

MODEL="llama3.1:latest"
OLLAMA_PORT="11434"
API_PORT="8080"
SKIP_OLLAMA=0
PULL=0
LISTEN_LAN=0
COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-}"
WAIT_SECONDS=120

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="${ROOT}/deploy/compose/docker-compose.ollama.yml"
OLLAMA_URL="http://127.0.0.1:${OLLAMA_PORT}"

usage() {
  echo "Usage: $0 [--model TAG] [--ollama-port N] [--api-port N] [--pull] [--skip-ollama] [--wait SEC] [--compose-project NAME] [--listen-lan]" >&2
  exit 2
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --model) MODEL="${2:-}"; shift 2 ;;
    --ollama-port) OLLAMA_PORT="${2:-}"; shift 2 ;;
    --api-port) API_PORT="${2:-}"; shift 2 ;;
    --pull) PULL=1; shift ;;
    --skip-ollama) SKIP_OLLAMA=1; shift ;;
    --wait) WAIT_SECONDS="${2:-}"; shift 2 ;;
    --compose-project) export COMPOSE_PROJECT_NAME="${2:-}"; shift 2 ;;
    --listen-lan) LISTEN_LAN=1; shift ;;
    -h|--help) usage ;;
    *) echo "Unknown option: $1" >&2; usage ;;
  esac
done

OLLAMA_URL="http://127.0.0.1:${OLLAMA_PORT}"
export ASHLAR_OLLAMA_HOST_PORT="${OLLAMA_PORT}"
cd "${ROOT}"

if [[ "${SKIP_OLLAMA}" -eq 0 ]]; then
  echo "Starting Ollama (Docker) on host port ${OLLAMA_PORT}..."
  docker compose -f "${COMPOSE_FILE}" up -d

  echo "Waiting for Ollama at ${OLLAMA_URL}..."
  ok=0
  for ((i = 0; i < WAIT_SECONDS; i++)); do
    if curl -sf "${OLLAMA_URL}/api/tags" >/dev/null 2>&1; then
      ok=1
      break
    fi
    sleep 1
  done
  if [[ "${ok}" -ne 1 ]]; then
    echo "Ollama did not become ready within ${WAIT_SECONDS}s. Try: docker compose -f deploy/compose/docker-compose.ollama.yml logs ollama" >&2
    exit 1
  fi
  echo "Ollama is ready."

  if [[ "${PULL}" -eq 1 ]]; then
    echo "Pulling model '${MODEL}'..."
    docker compose -f "${COMPOSE_FILE}" exec ollama ollama pull "${MODEL}"
  fi
else
  echo "Skip Ollama: using ${OLLAMA_URL}"
fi

export OLLAMA_BASE_URL="${OLLAMA_URL}"
export OLLAMA_MODEL="${MODEL}"
export Ashlar__NodeCapabilityRuntime__Ollama__BaseUrl="${OLLAMA_URL}"
if [[ "${LISTEN_LAN}" -eq 1 ]]; then
  export ASPNETCORE_URLS="http://0.0.0.0:${API_PORT}"
else
  export ASPNETCORE_URLS="http://127.0.0.1:${API_PORT}"
fi
unset ASHLAR_BACKGROUND_AGENTS_CONFIG 2>/dev/null || true

if [[ -z "${Ashlar__Security__ExposureProfile:-}" ]]; then
  if [[ "${LISTEN_LAN}" -eq 1 ]]; then
    export Ashlar__Security__ExposureProfile="Lan"
  else
    export Ashlar__Security__ExposureProfile="Localhost"
  fi
fi

# Ashlar.API refuses to start off-loopback with AuthorizationMode=None (fail closed). This dev helper is
# the one place that opts out explicitly: --listen-lan without an auth mode sets the documented escape
# hatch and says so. Prefer exporting Ashlar__Security__AuthorizationMode=ApiKey + Ashlar__Security__ApiKey.
if [[ "${LISTEN_LAN}" -eq 1 && -z "${Ashlar__Security__AuthorizationMode:-}" && -z "${Ashlar__Security__AllowUnauthenticatedNetworkExposure:-}" ]]; then
  export Ashlar__Security__AllowUnauthenticatedNetworkExposure="true"
  echo "WARNING: --listen-lan with no Ashlar__Security__AuthorizationMode: the LAN portal/API is UNAUTHENTICATED (AllowUnauthenticatedNetworkExposure=true set for this run)." >&2
  echo "         Set Ashlar__Security__AuthorizationMode=ApiKey and Ashlar__Security__ApiKey=<secret> to require a key instead." >&2
fi

LAN_IP=""
if command -v hostname >/dev/null 2>&1; then
  LAN_IP="$(hostname -I 2>/dev/null | awk '{print $1}')"
fi
if [[ -z "${LAN_IP}" ]] && command -v ipconfig >/dev/null 2>&1; then
  LAN_IP="$(ipconfig getifaddr en0 2>/dev/null || true)"
fi

echo ""
echo "Ashlar.API (this machine) → http://127.0.0.1:${API_PORT}  |  Ollama → ${OLLAMA_URL}  |  Model → ${MODEL}"
if [[ "${LISTEN_LAN}" -eq 1 ]]; then
  echo ""
  echo "LAN / phone (same Wi‑Fi): try http://${LAN_IP:-<this-host-LAN-IP>}:${API_PORT}"
  echo "Allow the port in your OS firewall if needed. Do not use --listen-lan on untrusted networks."
fi
echo ""
echo "Stop Ollama: docker compose -f deploy/compose/docker-compose.ollama.yml down"
echo "Or: bash scripts/stop-ashlar-api-dev.sh"
echo ""

dotnet run --project "${ROOT}/application/src/Ashlar.API/Ashlar.API.csproj" -f net10.0
