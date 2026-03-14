#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="${1:-$(pwd)}"
MODE="${2:-full}"
RELEASE_CORE_GOALS="${ROOT_DIR}/docs/runtime/benchmarks/release_core_goals.txt"
RELEASE_VISUAL_GOALS="${ROOT_DIR}/docs/runtime/benchmarks/release_visual_goals.txt"
CHAOS_GOALS="${ROOT_DIR}/docs/runtime/benchmarks/chaos_goals.txt"

CORE_MIN_PASS_RATE="${NEXO_RELEASE_CORE_MIN_PASS_RATE:-${NEXO_RELEASE_MIN_PASS_RATE:-0.85}}"
CORE_MIN_TOTAL="${NEXO_RELEASE_CORE_MIN_TOTAL:-${NEXO_RELEASE_MIN_TOTAL:-10}}"
CORE_HISTORY_WINDOW="${NEXO_RELEASE_CORE_HISTORY_WINDOW:-${NEXO_RELEASE_HISTORY_WINDOW:-20}}"

VISUAL_MIN_PASS_RATE="${NEXO_RELEASE_VISUAL_MIN_PASS_RATE:-0.80}"
VISUAL_MIN_TOTAL="${NEXO_RELEASE_VISUAL_MIN_TOTAL:-8}"
VISUAL_HISTORY_WINDOW="${NEXO_RELEASE_VISUAL_HISTORY_WINDOW:-20}"
VISUAL_PROMOTION_STREAK="${NEXO_VISUAL_PROMOTION_STREAK:-3}"
VISUAL_REQUIRED_MODE="${NEXO_VISUAL_REQUIRED_MODE:-auto}" # auto | true | false

PROVIDER="${NEXO_RELEASE_PROVIDER:-mock-json}"

if [[ ! -f "${RELEASE_CORE_GOALS}" ]]; then
  echo "release gate: missing release core goals file: ${RELEASE_CORE_GOALS}" >&2
  exit 1
fi

if [[ ! -f "${RELEASE_VISUAL_GOALS}" ]]; then
  echo "release gate: missing release visual goals file: ${RELEASE_VISUAL_GOALS}" >&2
  exit 1
fi

if [[ ! -f "${CHAOS_GOALS}" ]]; then
  echo "release gate: missing chaos goals file: ${CHAOS_GOALS}" >&2
  exit 1
fi

run_core_lane() {
  echo "=== Runtime Release Gate: release-core matrix ==="
  dotnet run --project "${ROOT_DIR}/src/Nexo.CLI" -- runtime evaluate \
    --goals-file "${RELEASE_CORE_GOALS}" \
    --policies release \
    --benchmark-set release-core \
    --repo-root "${ROOT_DIR}" \
    --provider "${PROVIDER}" \
    --allow-mock \
    --run-tests \
    --persist-history \
    --json

  echo "=== Runtime Release Gate: release-core SLO gate ==="
  dotnet run --project "${ROOT_DIR}/src/Nexo.CLI" -- runtime gate \
    --repo-root "${ROOT_DIR}" \
    --benchmark-set release-core \
    --policy release \
    --history-window "${CORE_HISTORY_WINDOW}" \
    --min-pass-rate "${CORE_MIN_PASS_RATE}" \
    --min-total "${CORE_MIN_TOTAL}" \
    --min-consecutive-passes 2 \
    --json
}

run_visual_lane() {
  local visual_required=0
  local visual_degrade_arg=()
  case "$(echo "${VISUAL_REQUIRED_MODE}" | tr '[:upper:]' '[:lower:]')" in
    true|1|yes|required) visual_required=1 ;;
    false|0|no|optional) visual_required=0 ;;
    auto)
      set +e
      dotnet run --project "${ROOT_DIR}/src/Nexo.CLI" -- runtime gate \
        --repo-root "${ROOT_DIR}" \
        --benchmark-set release-visual \
        --policy release \
        --history-window "${VISUAL_HISTORY_WINDOW}" \
        --min-pass-rate 0 \
        --min-total "${VISUAL_PROMOTION_STREAK}" \
        --min-consecutive-passes "${VISUAL_PROMOTION_STREAK}" \
        --json >/dev/null
      local streak_gate_exit=$?
      set -e
      if [[ ${streak_gate_exit} -eq 0 ]]; then
        visual_required=1
      fi
      ;;
    *)
      echo "release gate: invalid NEXO_VISUAL_REQUIRED_MODE='${VISUAL_REQUIRED_MODE}', expected auto|true|false" >&2
      exit 1
      ;;
  esac
  if [[ ${visual_required} -eq 0 ]]; then
    visual_degrade_arg=(--allow-visual-capability-degrade)
  fi

  echo "=== Runtime Release Gate: release-visual matrix ==="
  set +e
  dotnet run --project "${ROOT_DIR}/src/Nexo.CLI" -- runtime evaluate \
    --goals-file "${RELEASE_VISUAL_GOALS}" \
    --policies release \
    --benchmark-set release-visual \
    --repo-root "${ROOT_DIR}" \
    --provider "${PROVIDER}" \
    --allow-mock \
    --run-tests \
    --persist-history \
    "${visual_degrade_arg[@]}" \
    --json
  local visual_eval_exit=$?
  set -e

  if [[ ${visual_eval_exit} -ne 0 ]]; then
    if [[ ${visual_required} -eq 1 ]]; then
      echo "release gate: release-visual lane is required after green streak; matrix failed." >&2
      exit 1
    fi
    echo "release gate: release-visual matrix failed but lane remains advisory until streak ${VISUAL_PROMOTION_STREAK} is established." >&2
    return 0
  fi

  echo "=== Runtime Release Gate: release-visual SLO gate ==="
  set +e
  dotnet run --project "${ROOT_DIR}/src/Nexo.CLI" -- runtime gate \
    --repo-root "${ROOT_DIR}" \
    --benchmark-set release-visual \
    --policy release \
    --history-window "${VISUAL_HISTORY_WINDOW}" \
    --min-pass-rate "${VISUAL_MIN_PASS_RATE}" \
    --min-total "${VISUAL_MIN_TOTAL}" \
    --min-consecutive-passes "${VISUAL_PROMOTION_STREAK}" \
    --json
  local visual_gate_exit=$?
  set -e

  if [[ ${visual_gate_exit} -ne 0 ]]; then
    if [[ ${visual_required} -eq 1 ]]; then
      echo "release gate: release-visual lane is required after green streak; gate failed." >&2
      exit 1
    fi
    echo "release gate: release-visual lane remains advisory until streak ${VISUAL_PROMOTION_STREAK} is established." >&2
  fi
}

run_chaos_lane() {
  echo "=== Runtime Release Gate: chaos matrix (non-gating) ==="
  set +e
  dotnet run --project "${ROOT_DIR}/src/Nexo.CLI" -- runtime evaluate \
    --goals-file "${CHAOS_GOALS}" \
    --policies prod \
    --benchmark-set chaos \
    --repo-root "${ROOT_DIR}" \
    --provider "${PROVIDER}" \
    --allow-mock \
    --run-tests \
    --persist-history false \
    --json
  local chaos_exit=$?
  set -e

  if [[ ${chaos_exit} -ne 0 ]]; then
    echo "release gate: chaos matrix reported failures (expected in stress mode)." >&2
  fi
}

case "${MODE}" in
  core)
    run_core_lane
    ;;
  visual)
    run_visual_lane
    ;;
  chaos)
    run_chaos_lane
    ;;
  full)
    run_core_lane
    run_visual_lane
    run_chaos_lane
    ;;
  *)
    echo "release gate: unsupported mode '${MODE}'. Use core | visual | chaos | full." >&2
    exit 1
    ;;
esac

echo "=== Runtime Release Gate: PASSED ==="
