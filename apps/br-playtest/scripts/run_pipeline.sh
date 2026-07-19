#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
GAME_ROOT="${NEXO_BR_GAME_ROOT:-$(cd "$ROOT/.." && pwd)/NexoBattleRoyale}"
UNITY_BIN="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.5.3f1/Unity.app/Contents/MacOS/Unity}"
BUILD_PATH="${NEXO_BR_GAME_BUILD:-$GAME_ROOT/Builds/WeaponLab/NexoBRWeaponLab.app}"
SKIP_BUILD=0

if [[ "${1:-}" == "--skip-build" ]]; then
  SKIP_BUILD=1
  shift
fi

if [[ "$SKIP_BUILD" -eq 0 ]]; then
  if pgrep -f "Unity.*-projectPath $GAME_ROOT" >/dev/null; then
    if [[ -d "$BUILD_PATH" ]]; then
      echo "Unity editor is open; reusing existing Weapon Lab build."
    else
      echo "Unity editor is open and no playtest build exists. Close Unity or pass a valid NEXO_BR_GAME_BUILD." >&2
      exit 2
    fi
  else
    "$UNITY_BIN" -batchmode -nographics -quit \
      -projectPath "$GAME_ROOT" \
      -executeMethod BR.Game.Networking.Fusion.Editor.FusionPlayerPrefabScaffolder.ConfigureWeaponLabScene \
      -logFile /tmp/nexo-br-playtest-configure.log
    NEXO_BR_PLAYTEST_BUILD="$BUILD_PATH" \
      "$UNITY_BIN" -batchmode -nographics -quit \
      -projectPath "$GAME_ROOT" \
      -executeMethod BR.Game.Editor.WeaponLabBuildPipeline.BuildMacPlaytest \
      -logFile /tmp/nexo-br-playtest-build.log
  fi
fi

NEXO_BR_GAME_ROOT="$GAME_ROOT" \
NEXO_BR_GAME_BUILD="$BUILD_PATH" \
  "$ROOT/apps/br-playtest/scripts/run_once.sh" "$@"
