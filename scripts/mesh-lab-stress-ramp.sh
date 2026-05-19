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
# Workers have no published ports; bursts target replica IPv4 addresses on the lab bridge (Compose DNS for the
# hostname `worker` is unreliable from ephemeral curl containers on some Docker setups).
# GET /health is usually unauthenticated (MutatingApi scope); ramp still stress-tests connections + replica churn.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

COMPOSE_ENV_FILE="${1:?env file path required (e.g. .env.mesh-lab)}"
# Optional: export MESH_LAB_STRESS_NO_OVERRIDE=1 to use host-published worker port (single replica only).
STRESS_OVERRIDE="${MESH_LAB_STRESS_OVERRIDE_FILE:-docker-compose.mesh-lab-stress.override.yml}"
MAX_W="${2:-8}"
STEP="${3:-2}"
REQS="${4:-30}"
PAUSE="${5:-4}"
CURL_IMAGE="${MESH_LAB_CURL_IMAGE:-curlimages/curl:8.5.0}"

[[ -f "$COMPOSE_ENV_FILE" ]] || { echo "Missing $COMPOSE_ENV_FILE" >&2; exit 1; }

compose() {
  local -a files=(-f docker-compose.mesh-lab.yml)
  if [[ "${MESH_LAB_STRESS_NO_OVERRIDE:-}" != "1" && "${MESH_LAB_STRESS_NO_OVERRIDE:-}" != "true" && -f "$STRESS_OVERRIDE" ]]; then
    files+=(-f "$STRESS_OVERRIDE")
  fi
  docker compose --profile workers "${files[@]}" --env-file "$COMPOSE_ENV_FILE" "$@"
}

lab_network() {
  local cid nets pick
  cid="$(compose ps -q peer-a)"
  [[ -n "$cid" ]] || { echo "peer-a not running; start the lab first" >&2; exit 1; }
  nets="$(docker inspect "$cid" --format '{{range $k,$v := .NetworkSettings.Networks}}{{printf "%s\n" $k}}{{end}}')"
  pick="$(echo "$nets" | grep mesh_lab | head -1 | tr -d '\r\n')"
  if [[ -z "$pick" ]]; then
    pick="$(echo "$nets" | head -1 | tr -d '\r\n')"
  fi
  [[ -n "$pick" ]] || { echo "Could not resolve lab network from peer-a" >&2; exit 1; }
  echo "$pick"
}

worker_ips_on_net() {
  local peer_net=$1 wid ip
  [[ -n "$peer_net" ]] || return 1
  compose ps -q worker | while read -r wid; do
    [[ -z "$wid" ]] && continue
    ip="$(
      docker inspect "$wid" | python3 -c '
import json, re, sys

peer = sys.argv[1]
ns = json.load(sys.stdin)[0]["NetworkSettings"]["Networks"]
ordered = []
cfg = ns.get(peer)
if cfg is not None:
    ordered.append(cfg)
ordered.extend(v for k, v in ns.items() if k != peer)

for c in ordered:
    for raw in (c.get("IPAddress"), c.get("IPv4Address")):
        if not raw:
            continue
        s = str(raw).split("/", 1)[0].strip()
        if re.fullmatch(r"\d+\.\d+\.\d+\.\d+", s):
            print(s)
            raise SystemExit(0)
raise SystemExit(1)
' "$peer_net"
    )" || continue
    printf '%s\n' "$ip"
  done
}

parallel_health_burst() {
  local net=$1
  local count=$2
  local i
  local -a ips=()
  local tmpdir ok fail max_fail_pct min_ok
  tmpdir="$(mktemp -d "${TMPDIR:-/tmp}/nexo-stress.XXXXXX")"
  max_fail_pct="${MESH_LAB_STRESS_MAX_FAIL_PERCENT:-25}"
  min_ok="${MESH_LAB_STRESS_MIN_OK:-1}"
  while IFS= read -r ip; do
    [[ -n "$ip" ]] && ips+=("$ip")
  done < <(worker_ips_on_net "$net")
  if [[ "${#ips[@]}" -eq 0 ]]; then
    ips=("worker")
  fi
  local nReplicas=${#ips[@]}
  for ((i = 1; i <= count; i++)); do
    pick="${ips[$((RANDOM % nReplicas))]}"
    url="http://${pick}:8080/health"
    (
      if docker run --rm --network "$net" "$CURL_IMAGE" -fsS -m 20 "$url" >/dev/null 2>&1; then
        touch "${tmpdir}/ok.${i}"
      else
        touch "${tmpdir}/fail.${i}"
      fi
    ) &
    if (( i % 15 == 0 )); then
      wait || true
    fi
  done
  wait || true
  ok=$(find "$tmpdir" -name 'ok.*' 2>/dev/null | wc -l | tr -d '[:space:]')
  fail=$(find "$tmpdir" -name 'fail.*' 2>/dev/null | wc -l | tr -d '[:space:]')
  rm -rf "$tmpdir"
  echo "  burst: ok=${ok} fail=${fail} total=${count} (targets ${nReplicas} worker backend(s) on ${net})"
  if [[ "$count" -gt 0 ]]; then
    if [[ "$ok" -lt "$min_ok" ]]; then
      echo "Stress burst: fewer than ${min_ok} successful /health responses" >&2
      return 1
    fi
    if [[ "$((fail * 100))" -gt "$((count * max_fail_pct))" ]]; then
      echo "Stress burst: failure rate exceeds ${max_fail_pct}% (${fail}/${count})" >&2
      return 1
    fi
  fi
  return 0
}

echo "== Mesh lab stress ramp (worker replicas) =="
echo "max_workers=${MAX_W} step=${STEP} requests_per_step=${REQS} pause_sec=${PAUSE}"

NET="$(lab_network)"
echo "lab network: ${NET}"

echo "-- recreate worker tier without host port bindings (scale-safe) --"
compose stop worker 2>/dev/null || true
compose rm -f -s worker 2>/dev/null || true
compose up -d --no-deps --scale worker=1 worker

for ((w = STEP; w <= MAX_W; w += STEP)); do
  echo "-- scale worker -> ${w} --"
  compose up -d --no-deps --scale "worker=${w}" worker
  sleep "$PAUSE"
  echo "-- parallel /health burst --"
  if ! parallel_health_burst "$NET" "$REQS"; then
    exit 1
  fi
  sleep 1
done

echo "== Stress ramp complete =="
