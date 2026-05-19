#!/usr/bin/env bash
# curl against mesh-lab HTTPS director; macOS LibreSSL + Docker-published TLS often fails (bad decrypt).
# Falls back to OpenSSL curl in a container via host.docker.internal.

mesh_lab_tls_curl() {
  local tls_host="${MESH_LAB_TLS_HOST:-127.0.0.1:18443}"
  local tls_port="${tls_host##*:}"
  local curl_args=(--http1.1)
  if [[ -n "${MESH_LAB_TLS_CACERT:-}" ]]; then
    curl_args+=(--cacert "${MESH_LAB_TLS_CACERT}")
  else
    curl_args+=(-k)
  fi

  if curl -fsS "${curl_args[@]}" "$@" 2>/dev/null; then
    return 0
  fi

  if ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; then
    curl -fsS "${curl_args[@]}" "$@"
    return $?
  fi

  local rewritten=()
  local url
  for url in "$@"; do
    if [[ "$url" == https://127.0.0.1:* ]] || [[ "$url" == https://localhost:* ]]; then
      url="${url/https:\/\/127.0.0.1/https://host.docker.internal}"
      url="${url/https:\/\/localhost/https://host.docker.internal}"
    fi
    rewritten+=("$url")
  done

  docker run --rm curlimages/curl:8.5.0 -fsS "${curl_args[@]}" "${rewritten[@]}"
}

mesh_lab_tls_curl_status() {
  local tls_host="${MESH_LAB_TLS_HOST:-127.0.0.1:18443}"
  local out
  if out="$(curl -s -o /dev/null -w '%{http_code}' --http1.1 -k "$@" 2>/dev/null)"; then
    echo "$out"
    return 0
  fi
  if command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
    local rewritten=()
    local url
    for url in "$@"; do
      if [[ "$url" == https://127.0.0.1:* ]] || [[ "$url" == https://localhost:* ]]; then
        url="${url/https:\/\/127.0.0.1/https://host.docker.internal}"
        url="${url/https:\/\/localhost/https://host.docker.internal}"
      fi
      rewritten+=("$url")
    done
    docker run --rm curlimages/curl:8.5.0 -s -o /dev/null -w '%{http_code}' --http1.1 -k "${rewritten[@]}"
    return $?
  fi
  curl -s -o /dev/null -w '%{http_code}' --http1.1 -k "$@"
}
