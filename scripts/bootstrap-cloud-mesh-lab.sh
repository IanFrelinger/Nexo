#!/usr/bin/env bash
# Bootstrap a Linux host (e.g. cloud VM) for the virtual mesh lab: Docker + Compose,
# env file, build/up, verify. Run from the Ashlar repository root after clone.
#
# Usage:
#   chmod +x scripts/bootstrap-cloud-mesh-lab.sh
#   ./scripts/bootstrap-cloud-mesh-lab.sh           # create .env.mesh-lab if missing, up + verify
#   ./scripts/bootstrap-cloud-mesh-lab.sh --install-docker   # apt-only: install docker.io + compose v2
#
# See docs/MeshVirtualLab.md

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "${REPO_ROOT}"

INSTALL_DOCKER=false
SKIP_VERIFY=false
WITH_WORKERS=false
WITH_DEEP=false

usage() {
  echo "Usage: $0 [--install-docker] [--skip-verify] [--workers] [--deep]"
  echo "  --install-docker   On Debian/Ubuntu: apt-get install docker.io + compose v2 (requires sudo)."
  echo "  --skip-verify      Only docker compose up (no mesh-lab-verify.sh)."
  echo "  --workers          Include Compose profile workers (executor + auth checks in verify)."
  echo "  --deep             After standard verify, run mesh-lab-verify-deep.sh (requires --workers for full CI parity)."
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --install-docker) INSTALL_DOCKER=true ;;
    --skip-verify) SKIP_VERIFY=true ;;
    --workers) WITH_WORKERS=true ;;
    --deep) WITH_DEEP=true; WITH_WORKERS=true ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1" >&2; usage; exit 1 ;;
  esac
  shift
done

run_sudo() {
  if [[ "${EUID}" -eq 0 ]]; then
    "$@"
  elif command -v sudo >/dev/null 2>&1; then
    sudo "$@"
  else
    echo "Root or sudo required for: $*" >&2
    exit 1
  fi
}

install_docker_apt() {
  if command -v docker >/dev/null 2>&1 && docker compose version >/dev/null 2>&1; then
    echo "Docker and docker compose already available."
    return 0
  fi
  if ! command -v apt-get >/dev/null 2>&1; then
    echo "This script only auto-installs Docker on apt-based systems. Install Docker Engine + Compose v2 manually:" >&2
    echo "  https://docs.docker.com/engine/install/" >&2
    exit 1
  fi
  echo "Installing docker.io and docker-compose-v2 (Ubuntu/Debian)..."
  run_sudo apt-get update
  run_sudo apt-get install -y docker.io docker-compose-v2
  if command -v systemctl >/dev/null 2>&1; then
    run_sudo systemctl enable --now docker || true
  fi
}

ensure_compose() {
  if command -v docker >/dev/null 2>&1 && docker compose version >/dev/null 2>&1; then
    return 0
  fi
  echo "Docker Compose v2 is required ('docker compose version')." >&2
  echo "  Debian/Ubuntu: sudo apt-get install -y docker.io docker-compose-v2" >&2
  echo "  Or run this script with: --install-docker" >&2
  exit 1
}

ensure_env_file() {
  local example="${REPO_ROOT}/docs/config/mesh-lab.env.example"
  local target="${REPO_ROOT}/.env.mesh-lab"
  if [[ -f "${target}" ]]; then
    echo "Using existing ${target}"
    return 0
  fi
  if [[ ! -f "${example}" ]]; then
    echo "Missing ${example}" >&2
    exit 1
  fi
  cp "${example}" "${target}"
  # Non-cryptographic placeholders for a throwaway lab (override for real secrets).
  local k b w
  k="$(openssl rand -hex 16 2>/dev/null || printf '%s' "lab-key-change-me")"
  b="$(openssl rand -hex 16 2>/dev/null || printf '%s' "lab-bearer-change-me")"
  w="$(openssl rand -hex 12 2>/dev/null || printf '%s' "lab-basic-change-me")"
  sed -i.bak \
    -e "s/^Ashlar__Security__ApiKey=.*/Ashlar__Security__ApiKey=${k}/" \
    -e "s/^Ashlar__Security__PeerB__BearerToken=.*/Ashlar__Security__PeerB__BearerToken=${b}/" \
    -e "s/^Ashlar__Security__Worker__BasicAuthPassword=.*/Ashlar__Security__Worker__BasicAuthPassword=${w}/" \
    "${target}" && rm -f "${target}.bak"
  echo "Created ${target} with random lab secrets (review before production use)."
}

main() {
  if [[ "${INSTALL_DOCKER}" == "true" ]]; then
    install_docker_apt
  fi
  ensure_compose

  ensure_env_file

  echo "Building and starting mesh lab (deploy/compose/docker-compose.mesh-lab.yml)..."
  WORKERS_PROFILE_ARGS=()
  if [[ "${WITH_WORKERS}" == "true" ]]; then
    WORKERS_PROFILE_ARGS=(--profile workers)
  fi
  docker compose "${WORKERS_PROFILE_ARGS[@]}" -f deploy/compose/docker-compose.mesh-lab.yml --env-file .env.mesh-lab up -d --build

  if [[ "${SKIP_VERIFY}" == "true" ]]; then
    echo "Skip verify (--skip-verify). Peers: http://127.0.0.1:18081 and :18082 on this host."
    exit 0
  fi

  echo "Running mesh-lab-verify.sh ..."
  bash "${REPO_ROOT}/scripts/mesh-lab-verify.sh" "${REPO_ROOT}/.env.mesh-lab"

  if [[ "${WITH_DEEP}" == "true" ]]; then
    echo "Running mesh-lab-verify-deep.sh ..."
    bash "${REPO_ROOT}/scripts/mesh-lab-verify-deep.sh" "${REPO_ROOT}/.env.mesh-lab"
  fi

  echo ""
  echo "Mesh lab is up. On this machine:"
  echo "  peer-a: http://127.0.0.1:18081"
  echo "  peer-b: http://127.0.0.1:18082"
  echo "SSH tunnel from laptop: ssh -L 18081:127.0.0.1:18081 -L 18082:127.0.0.1:18082 user@vm"
  echo "Stop: docker compose --profile workers -f deploy/compose/docker-compose.mesh-lab.yml --env-file .env.mesh-lab down -v"
}

main
