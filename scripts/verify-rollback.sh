#!/usr/bin/env bash
set -euo pipefail
root="${1:?rehearsal root}"
lock="${2:?previous lock}"
cd "$root"
set -a; . "$lock"; set +a
export COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-nexo-release-rehearsal}"
docker compose -f docker-compose.deploy.yml up -d --force-recreate dep-extract dep-extract-agent
for _ in $(seq 1 60); do
  curl -fsS "http://127.0.0.1:${AGENT_PORT:-5237}/api/status" > .nexo/rollback-status.json && break
  sleep 2
done
python3 - <<'PY'
import json
d=json.load(open(".nexo/rollback-status.json"))
assert d["extractReady"] and d["adaptReady"], d
PY
docker inspect "${COMPOSE_PROJECT_NAME}-dep-extract-agent-1" |
  python3 -c 'import json,sys; print(json.load(sys.stdin)[0]["Image"])' > .nexo/rollback-agent-image.txt
echo ROLLBACK_PASS
