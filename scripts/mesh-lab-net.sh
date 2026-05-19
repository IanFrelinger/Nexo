#!/usr/bin/env bash
# Shared Docker network helpers for mesh lab verify scripts.

mesh_lab_compose() {
  docker compose -f "${COMPOSE_FILE:-docker-compose.mesh-lab.yml}" --env-file "${COMPOSE_ENV_FILE:?}" "$@"
}

mesh_lab_network() {
  local cid nets pick
  cid="$(mesh_lab_compose ps -q peer-a)"
  if [[ -z "$cid" ]]; then
    echo "peer-a container not running" >&2
    return 1
  fi
  nets="$(docker inspect "$cid" --format '{{range $k,$v := .NetworkSettings.Networks}}{{printf "%s\n" $k}}{{end}}')"
  pick="$(echo "$nets" | grep mesh_lab | head -1 | tr -d '\r\n')"
  if [[ -z "$pick" ]]; then
    pick="$(echo "$nets" | head -1 | tr -d '\r\n')"
  fi
  if [[ -z "$pick" ]]; then
    echo "Could not determine Docker network name from peer-a" >&2
    return 1
  fi
  echo "$pick"
}

# curl on lab bridge; extra args before URL (e.g. -H headers).
mesh_lab_curl_on_net() {
  local net
  net="$(mesh_lab_network)" || return 1
  docker run --rm --network "$net" "${MESH_LAB_CURL_IMAGE:-curlimages/curl:8.5.0}" "$@"
}

# Expect non-zero exit (connection/DNS failure). Uses short connect timeout.
mesh_lab_curl_on_net_must_fail() {
  local url=$1
  if mesh_lab_curl_on_net --connect-timeout 5 --max-time 10 -fsS "$url" >/dev/null 2>&1; then
    echo "Expected curl to fail for ${url}" >&2
    return 1
  fi
  return 0
}

mesh_lab_wait_peer_a() {
  local host="${MESH_LAB_PEER_A_HOST:-127.0.0.1:18081}"
  local i
  for i in $(seq 1 "${MESH_LAB_WAIT_PEER_A_SEC:-60}"); do
    if curl -fsS -m 3 "http://${host}/health" >/dev/null 2>&1; then
      return 0
    fi
    sleep 1
  done
  echo "peer-a not healthy at http://${host}/health" >&2
  return 1
}
