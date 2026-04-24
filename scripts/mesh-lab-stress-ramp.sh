#!/usr/bin/env bash
# Scale the mesh-lab worker tier over time and fire parallel /health requests each step.
#
# Prerequisite: lab stack running including workers:
#   docker compose -f docker-compose.mesh-lab.yml --env-file .env.mesh-lab up -d --build
#
# Usage:
#   ./scripts/mesh-lab-stress-ramp.sh .env.mesh-lab [max_workers] [step] [requests_per_step] [pause_sec]
# Defaults: max_workers=8 step=2 requests_per_step=30 pause_sec=4
#
# "worker" is a scalable service (no host ports). Docker DNS round-robins http://worker:8080 across replicas.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.mesh-lab.yml}"
COMPOSE_ENV_FILE="${1:?env file path required (e.g. .env.mesh-lab)}"
MAX_W="${2:-8}"
STEP="${3:-2}"
REQS="${4:-30}"
PAUSE="${5:-4}"
CURL_IMAGE="${MESH_LAB_CURL_IMAGE:-curlimages/curl:8.5.0}"

[[ -f "$COMPOSE_ENV_FILE" ]] || { echo "Missing $COMPOSE_ENV_FILE" >&2; exit 1; }

compose() {
  docker compose --profile workers -f "$COMPOSE_FILE" --env-file "$COMPOSE_ENV_FILE" "$@"
}

lab_network() {
  local cid
  cid="$(compose ps -q peer-a)"
  [[ -n "$cid" ]] || { echo "peer-a not running; start the lab first" >&2; exit 1; }
  docker inspect "$cid" --format '{{range $k,$v := .NetworkSettings.Networks}}{{$k}} {{end}}' | awk '{print $1; exit}'
}

parallel_health_burst() {
  local net=$1
  local count=$2
  local ok=0
  local fail=0
  local i
  for ((i = 1; i <= count; i++)); do
    if docker run --rm --network "$net" "$CURL_IMAGE" -fsS -m 20 "http://worker:8080/health" >/dev/null 2>&1; then
      ((ok++)) || true
    else
      ((fail++)) || true
    fi &
    # throttle fork rate slightly on constrained runners
    if (( i % 15 == 0 )); then
      wait || true
    fi
  done
  wait || true
  echo "  burst: ok=${ok} fail=${fail} total=${count} (target http://worker:8080/health)"
}

echo "== Mesh lab stress ramp (worker replicas) =="
echo "max_workers=${MAX_W} step=${STEP} requests_per_step=${REQS} pause_sec=${PAUSE}"

NET="$(lab_network)"
echo "lab network: ${NET}"

compose up -d --no-deps --scale worker=1 worker

for ((w = STEP; w <= MAX_W; w += STEP)); do
  echo "-- scale worker -> ${w} --"
  compose up -d --no-deps --scale "worker=${w}" worker
  sleep "$PAUSE"
  echo "-- parallel /health burst --"
  parallel_health_burst "$NET" "$REQS"
  sleep 1
done

echo "== Stress ramp complete =="
