#!/usr/bin/env bash
# Roll the committed deploy/node.yml pin out to every box in deploy/fleet.env, then print what
# each node actually RESOLVED — repo digest and architecture — because an Apple Silicon box
# that silently pulled amd64 runs the fleet under emulation and reports nothing.
#
# deploy/fleet.env, one row per box (explicit IPs or hostnames, never .local names):
#
#   <name> <ssh-target> <absolute-repo-path>
#   winbox  ian@192.168.1.20  /c/Users/icfre/Downloads/Nexo
#   macbook ian@192.168.1.21  /Users/ian/Nexo
#
# Comment rows out with '#'. See CLOSING-PLAN Phase 4 step 1 for why this file is
# hand-maintained rather than discovered.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
FLEET_ENV="${REPO_ROOT}/deploy/fleet.env"

if [ ! -f "${FLEET_ENV}" ]; then
  echo "no deploy/fleet.env yet — write one row per box: <name> <ssh-target> <repo-path>" >&2
  echo "(CLOSING-PLAN Phase 4 step 1; that decision is deliberately yours, not this script's)" >&2
  exit 1
fi

FAILED=0
while read -r NAME TARGET RPATH; do
  case "${NAME}" in ''|'#'*) continue ;; esac
  echo "== ${NAME} (${TARGET}) =="
  if ! ssh -n -o BatchMode=yes "${TARGET}" \
      "cd '${RPATH}' && git pull --ff-only && docker compose -f deploy/node.yml pull -q && docker compose -f deploy/node.yml up -d"; then
    echo "   FAILED: update on ${NAME}" >&2
    FAILED=1
    continue
  fi
  ssh -n -o BatchMode=yes "${TARGET}" \
    "cd '${RPATH}' && CID=\$(docker compose -f deploy/node.yml ps -q node) && IMG=\$(docker inspect --format '{{.Image}}' \"\${CID}\") && docker image inspect --format '   resolved: {{index .RepoDigests 0}}  arch: {{.Os}}/{{.Architecture}}' \"\${IMG}\"" \
    || { echo "   FAILED: could not read resolved digest on ${NAME}" >&2; FAILED=1; }
done < "${FLEET_ENV}"

exit "${FAILED}"
