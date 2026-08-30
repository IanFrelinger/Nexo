#!/usr/bin/env bash
# `ashlar` on the host. One box, ONE operator identity: every invocation runs against the
# node's state volume, never against a host build's ~/.ashlar — a host build resolves
# OperatorKey.ResolveKeyDir() to the host HOME, silently giving the box a second identity,
# and the two-machine key ceremony then fails in the most confusing way available.
#
# Resolution order:
#   1. `docker exec` into the RUNNING node (compose service `node`).
#   2. The node exists but is stopped: a one-shot container on the node's OWN image with the
#      node's volume — the pin in deploy/node.yml stays the single source of image truth.
#   3. No node at all: refuse, and say what to run. Guessing an image here would un-pin
#      what deploy/node.yml pins.
set -euo pipefail

# Git Bash on Windows rewrites arguments that look like POSIX paths (/app/Ashlar.CLI.dll ->
# C:/Program Files/Git/app/Ashlar.CLI.dll) before docker sees them. Inert everywhere else.
export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL='*'

TTY_FLAGS=(-i)
if [ -t 0 ] && [ -t 1 ]; then
  TTY_FLAGS=(-it)
fi

CID="$(docker ps -q --filter "label=com.docker.compose.service=node" | head -n1)"
if [ -n "${CID}" ]; then
  exec docker exec "${TTY_FLAGS[@]}" "${CID}" dotnet /app/Ashlar.CLI.dll "$@"
fi

CID="$(docker ps -aq --filter "label=com.docker.compose.service=node" | head -n1)"
if [ -n "${CID}" ]; then
  IMG="$(docker inspect --format '{{.Config.Image}}' "${CID}")"
  exec docker run --rm "${TTY_FLAGS[@]}" \
    -v ashlar-state:/data/state \
    -w /data/state/project \
    "${IMG}" "$@"
fi

echo "ashlar: no node on this machine. Start one first, from your Ashlar checkout:" >&2
echo "  docker compose -f deploy/node.yml up -d" >&2
exit 1
