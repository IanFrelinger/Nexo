#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# live-selfextend.sh — prove the autonomy loop against a REAL model, in containers.
#
# Stands up an Ollama sidecar on a private docker network, pulls a small code model,
# and drives `ashlar self-extend run` through it — so you can watch a live model
# propose real tool calls and the governance floor govern them, WITHOUT deploying to
# your fleet. Container-first verification (roadmap A1).
#
#   Usage:  bash live-selfextend.sh [CLI_IMAGE] [MODEL]
#   e.g.    bash live-selfextend.sh ghcr.io/ianfrelinger/nexo-cli:latest qwen2.5-coder:1.5b
#
# What "working" looks like: the log shows `proposed N tool call(s)` with a real
# rationale (a live model, not the echo fallback), and the write floor refusing any
# disallowed target. A small model may fail to land an admissible change (EXIT=1) —
# that is model competence, not a wiring failure; a bigger model raises the hit rate.
# ─────────────────────────────────────────────────────────────────────────────
set -u
export MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*'

IMG="${1:-ghcr.io/ianfrelinger/nexo-cli:latest}"
MODEL="${2:-qwen2.5-coder:1.5b}"
NET="ashlar-live-net"
OLLAMA="ashlar-live-ollama"
STATE="ashlar-live-state"

cleanup() { docker rm -f "$OLLAMA" >/dev/null 2>&1; docker volume rm "$STATE" >/dev/null 2>&1;
            docker network rm "$NET" >/dev/null 2>&1; }
trap cleanup EXIT

echo "== 1. network + ollama sidecar =="
docker network create "$NET" >/dev/null 2>&1 || true
docker rm -f "$OLLAMA" >/dev/null 2>&1 || true
docker run -d --name "$OLLAMA" --network "$NET" ollama/ollama >/dev/null
# wait for the daemon
for i in $(seq 1 30); do
  docker exec "$OLLAMA" sh -c 'curl -sf http://localhost:11434/ >/dev/null 2>&1' && break; sleep 1
done
echo "   ollama up"

echo "== 2. pull $MODEL (first run downloads ~1GB) =="
docker exec "$OLLAMA" ollama pull "$MODEL" 2>&1 | tail -1

echo "== 3. a node on the same network, keys + project =="
R() { docker run --rm --network "$NET" \
        -e ASHLAR_KEY_DIR=/data/state/keys \
        -e ASHLAR_OLLAMA_BASE_URL="http://$OLLAMA:11434" \
        -e ASHLAR_OLLAMA_MODEL="$MODEL" \
        -v "$STATE":/data/state -w /data/state "$IMG" "$@"; }
R keys init >/dev/null 2>&1
R init live --path /data/state/proj >/dev/null 2>&1

echo "== 4. self-extend against the LIVE model (provider=ollama, allow-mock false) =="
# allow-mock false: if the model were NOT reached, A0 makes this fail hard rather than fake success.
R self-extend run \
    --goal "add a brick that returns the current UTC timestamp as a string" \
    --repo-root /data/state/proj --provider ollama --allow-mock false --max-iterations 1 \
    2>&1 | grep -iE "proposed .* tool call|Rationale|not allowed|ReAct cycle complete|self-extend:|executed:|ModelUnavailable" | head -20
echo
echo "If you saw 'proposed N tool call(s)' with a rationale, the loop ran on a real model."
echo "'not allowed' lines are the governance floor doing its job against the live model."
