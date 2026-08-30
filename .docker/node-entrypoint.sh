#!/bin/sh
# Node entrypoint: CLI passthrough for everything, boot sequence for the daemon.
#
# `docker run <image> keys show` must behave exactly as it did when the ENTRYPOINT was the bare
# CLI, so the boot steps run ONLY when the container is asked to be the node — first argument
# `background-agent`, which is what deploy/node.yml passes. Everything else execs straight
# through. (`docker exec` never enters this file at all.)
#
# Boot steps, in order:
#
#   1. A bounded wait for a plausible clock. A machine with no RTC, or a VM resumed with a stale
#      clock, can start dockerd before time syncs — and gate decisions and certification records
#      are timestamped, so "not before the image was built" is the cheapest sanity floor there
#      is. Skipped when IMAGE_BUILD_EPOCH is empty (local builds); after 120s the node continues
#      loudly rather than parking — the daemon's own guards own clock policy from there.
#
#   2. First-run project scaffold. The pkg/admission surface hard-requires ashlar.yaml AND
#      ashlar.policy.yaml in the project directory (PkgCommand refuses with "not an ashlar
#      project" otherwise), and nothing else creates them on a node. `ashlar init` refuses to
#      overwrite, so re-running is safe by construction; the file guard just keeps restarts
#      quiet. The scaffold ships `self-extend: sealed` and the daemon defaults to Passive —
#      a fresh node does no self-extension until the operator flips both, deliberately.
set -eu

CLI="/app/Ashlar.CLI.dll"
PROJECT_DIR="${ASHLAR_PROJECT_DIR:-/data/state/project}"

if [ "${1:-}" = "background-agent" ]; then
  if [ -n "${IMAGE_BUILD_EPOCH:-}" ]; then
    waited=0
    while [ "$(date +%s)" -lt "${IMAGE_BUILD_EPOCH}" ] && [ "${waited}" -lt 120 ]; do
      if [ "${waited}" -eq 0 ]; then
        echo "node-entrypoint: clock ($(date -u +%Y-%m-%dT%H:%M:%SZ)) predates the image build; waiting for sync (max 120s)" >&2
      fi
      sleep 5
      waited=$((waited + 5))
    done
    if [ "$(date +%s)" -lt "${IMAGE_BUILD_EPOCH}" ]; then
      echo "node-entrypoint: clock still implausible after ${waited}s; continuing anyway — timestamps on records minted now will predate the build" >&2
    fi
  fi

  if [ ! -f "${PROJECT_DIR}/ashlar.yaml" ] || [ ! -f "${PROJECT_DIR}/ashlar.policy.yaml" ]; then
    echo "node-entrypoint: no project at ${PROJECT_DIR}; scaffolding (ashlar init node)" >&2
    dotnet "${CLI}" init node --path "${PROJECT_DIR}" >&2 \
      || echo "node-entrypoint: init refused or failed; the daemon will start but the pkg surface will refuse until a project exists at ${PROJECT_DIR}" >&2
  fi
fi

exec dotnet "${CLI}" "$@"
