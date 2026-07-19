#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
GAME_ROOT="${NEXO_BR_GAME_ROOT:-$(cd "$ROOT/.." && pwd)/NexoBattleRoyale}"
GOAL="${*:-Create one balanced weapon proposal requested by the current Weapon Lab backlog. Read weapon.schema.json and existing descriptors first. Write .nexo/br-director/proposals/<proposal-id>/proposal.json plus descriptor.weapon.json. Do not modify live descriptors.}"

export NEXO_SANDBOX_ROOT="$GAME_ROOT"
export NEXO_PATH_ALLOWLIST_EXTRA=".nexo/br-director/proposals/"
export NEXO_MODEL_PROVIDER="${NEXO_MODEL_PROVIDER:-ollama}"

cd "$ROOT"
dotnet run --project application/src/Nexo.CLI -- self-extend run \
  --goal "$GOAL" \
  --repo-root "$GAME_ROOT" \
  --provider ollama \
  --allow-mock false
