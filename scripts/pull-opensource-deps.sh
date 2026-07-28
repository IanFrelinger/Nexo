#!/usr/bin/env bash
# Pull/configure open-source images + model from opensource.deps.lock.
# Idempotent: skips pulls when the image/model is already present.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOCK="${OPENSOURCE_DEPS_LOCK:-$ROOT/opensource.deps.lock}"
DRY_RUN=0
WITH_MODEL=1
while [ $# -gt 0 ]; do
  case "$1" in
    --dry-run) DRY_RUN=1; shift ;;
    --no-model) WITH_MODEL=0; shift ;;
    --lock) LOCK="${2:?}"; shift 2 ;;
    *) echo "usage: $0 [--dry-run] [--no-model] [--lock PATH]" >&2; exit 2 ;;
  esac
done

[ -f "$LOCK" ] || { echo "FAIL: missing $LOCK" >&2; exit 1; }
# shellcheck disable=SC1090
set -a; . "$LOCK"; set +a
OLLAMA_IMAGE="${OLLAMA_IMAGE//$'\r'/}"
DOCKER_CLI_IMAGE="${DOCKER_CLI_IMAGE//$'\r'/}"
UBUNTU_IMAGE="${UBUNTU_IMAGE//$'\r'/}"
OLLAMA_MODEL="${OLLAMA_MODEL//$'\r'/}"

need() {
  local key="$1" val="$2"
  [ -n "$val" ] || { echo "FAIL: $key empty in $LOCK" >&2; exit 1; }
}
need OLLAMA_IMAGE "$OLLAMA_IMAGE"
need DOCKER_CLI_IMAGE "$DOCKER_CLI_IMAGE"
need UBUNTU_IMAGE "$UBUNTU_IMAGE"
need OLLAMA_MODEL "$OLLAMA_MODEL"

pull_one() {
  local ref="$1" label="$2"
  if docker image inspect "$ref" >/dev/null 2>&1; then
    echo "  present: $label → $ref"
    return 0
  fi
  if [[ "$ref" == *@sha256:* ]]; then
    local digest="${ref##*@}"
    if docker images --digests --format '{{.Digest}}' | grep -qx "$digest"; then
      echo "  present (digest): $label → $ref"
      return 0
    fi
  fi
  if [ "$DRY_RUN" = 1 ]; then
    echo "  would-pull: $label → $ref"
    return 0
  fi
  echo "  pulling: $label → $ref"
  docker pull "$ref"
}

echo "==> open-source deps from $LOCK"
pull_one "$OLLAMA_IMAGE" ollama
pull_one "$DOCKER_CLI_IMAGE" docker-cli
pull_one "$UBUNTU_IMAGE" ubuntu

if [ "$DRY_RUN" = 0 ]; then
  docker tag "$OLLAMA_IMAGE" ollama/ollama:latest 2>/dev/null || true
  docker tag "$DOCKER_CLI_IMAGE" docker:27-cli 2>/dev/null || true
  docker tag "$UBUNTU_IMAGE" ubuntu:26.04 2>/dev/null || true
fi

if [ "$WITH_MODEL" = 1 ]; then
  if [ "$DRY_RUN" = 1 ]; then
    echo "  would-ensure-model: $OLLAMA_MODEL"
  else
    vol="${OLLAMA_VOLUME_NAME:-nexo-ollama-models}"
    docker volume create "$vol" >/dev/null 2>&1 || true
    cid="$(docker run -d -v "$vol":/root/.ollama "$OLLAMA_IMAGE")"
    cleanup_ollama() { docker rm -f "$cid" >/dev/null 2>&1 || true; }
    trap cleanup_ollama EXIT
    for _ in $(seq 1 40); do
      docker exec "$cid" ollama list >/dev/null 2>&1 && break
      sleep 2
    done
    if docker exec "$cid" ollama list 2>/dev/null | grep -qF "$OLLAMA_MODEL"; then
      echo "  present: model $OLLAMA_MODEL (volume $vol)"
    else
      echo "  pulling model: $OLLAMA_MODEL into volume $vol"
      docker exec "$cid" ollama pull "$OLLAMA_MODEL"
    fi
    cleanup_ollama
    trap - EXIT
  fi
fi

export OLLAMA_IMAGE DOCKER_CLI_IMAGE UBUNTU_IMAGE OLLAMA_MODEL
echo "PASS: open-source deps ready (model=$OLLAMA_MODEL)"
