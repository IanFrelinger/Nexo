#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
GAME_ROOT="${NEXO_BR_GAME_ROOT:-$(cd "$ROOT/.." && pwd)/NexoBattleRoyale}"

export NEXO_BR_GAME_ROOT="$GAME_ROOT"
export NEXO_BR_GAME_BUILD="${NEXO_BR_GAME_BUILD:-$GAME_ROOT/Builds/WeaponLab/NexoBRWeaponLab.app}"
export NEXO_BR_PLAYTEST_ARTIFACT_ROOT="${NEXO_BR_PLAYTEST_ARTIFACT_ROOT:-$ROOT/.nexo/playtest/br-weapon-lab}"
export NEXO_BR_PLAYTEST_PORT="${NEXO_BR_PLAYTEST_PORT:-17420}"

cd "$ROOT"
dotnet run --project tools/Nexo.BRPlaytestAgent/Nexo.BRPlaytestAgent.csproj -- "$@"
