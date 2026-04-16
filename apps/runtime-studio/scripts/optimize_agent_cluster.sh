#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
AGENT_SET_CONFIG="${REPO_ROOT}/apps/runtime-studio/config/agent_set.local.json"
SPEC_PATH=""
OBJECTIVE=""
OBJECTIVE_FILE=""
SEARCH_STRATEGY="successive-halving"
MAX_CANDIDATES=24
DURATION=""
DISABLE_OBSERVATION=0
FORMAT_JSON=0
SKIP_OPTIMIZE=0
SKIP_DAEMON=0
REPORT_OUTPUT=""
PROVIDER=""
PREFER=""
OLLAMA_MODEL_OVERRIDE=""
VERBOSE=0

usage() {
  cat <<EOF
Usage: $(basename "$0") [OPTIONS]

Unified workflow: bootstrap → scaffold spec → optimize compositions for your
hardware → (optionally) launch the background agent daemon with the tuned config.

Steps executed:
  1. Bootstrap Runtime Studio sandbox (idempotent).
  2. Scaffold a workflow lab spec if none exists.
  3. Run \`nexo workflow optimize\` to benchmark composition/model candidates
     on local hardware, select a winner, and emit a recommendation report.
  4. (Optional) Start the background agent daemon with the agent-set config.

Options:
  --objective <text>          High-level objective for candidate prioritisation.
  --objective-file <path>     File containing objective text.
  --spec <path>               Workflow lab runtime spec JSON
                              (default: .nexo/workflow/workflow_lab.runtime.json).
  --search-strategy <name>    successive-halving | objective-first | exhaustive
                              (default: successive-halving).
  --max-candidates <n>        Maximum candidates to evaluate (default: 24).
  --report-output <path>      Write recommendation report (.md/.json/.txt).
  --provider <name>           Override LLM provider for all profile entries.
  --prefer <mode>             Override model preference (agentic|deterministic|auto).
  --ollama-model <model>      Override OLLAMA_MODEL env var.
  --duration <dur>            Daemon duration (e.g. 5m, 1h). If omitted the
                              daemon step is skipped.
  --config <path>             Agent-set JSON for the daemon step
                              (default: apps/runtime-studio/config/agent_set.local.json).
  --skip-optimize             Skip the optimize step (bootstrap + daemon only).
  --skip-daemon               Skip the daemon step (bootstrap + optimize only).
  --disable-observation       Disable observation pipeline in the daemon.
  --format-json               Emit machine-readable JSON from the daemon.
  --verbose                   Emit progress output from the optimizer.
  -h, --help                  Show this help message and exit.
EOF
  exit 0
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --objective)       OBJECTIVE="${2:-}";        shift 2 ;;
    --objective-file)  OBJECTIVE_FILE="${2:-}";   shift 2 ;;
    --spec)            SPEC_PATH="${2:-}";        shift 2 ;;
    --search-strategy) SEARCH_STRATEGY="${2:-}";  shift 2 ;;
    --max-candidates)  MAX_CANDIDATES="${2:-24}"; shift 2 ;;
    --report-output)   REPORT_OUTPUT="${2:-}";    shift 2 ;;
    --provider)        PROVIDER="${2:-}";         shift 2 ;;
    --prefer)          PREFER="${2:-}";           shift 2 ;;
    --ollama-model)    OLLAMA_MODEL_OVERRIDE="${2:-}"; shift 2 ;;
    --duration)        DURATION="${2:-}";         shift 2 ;;
    --config)          AGENT_SET_CONFIG="${2:-}"; shift 2 ;;
    --skip-optimize)   SKIP_OPTIMIZE=1;          shift ;;
    --skip-daemon)     SKIP_DAEMON=1;            shift ;;
    --disable-observation) DISABLE_OBSERVATION=1; shift ;;
    --format-json)     FORMAT_JSON=1;            shift ;;
    --verbose)         VERBOSE=1;                shift ;;
    -h|--help)         usage ;;
    *)
      echo "Unknown argument: $1" >&2
      echo "Run with --help for usage." >&2
      exit 2
      ;;
  esac
done

# If no --duration and no --skip-daemon, default to skipping the daemon so
# the user doesn't accidentally start a long-lived process.
if [[ -z "${DURATION}" && "${SKIP_DAEMON}" -eq 0 ]]; then
  SKIP_DAEMON=1
fi

export OLLAMA_BASE_URL="${OLLAMA_BASE_URL:-http://127.0.0.1:11434}"
if [[ -n "${OLLAMA_MODEL_OVERRIDE}" ]]; then
  export OLLAMA_MODEL="${OLLAMA_MODEL_OVERRIDE}"
else
  export OLLAMA_MODEL="${OLLAMA_MODEL:-llama3.1}"
fi

# ── Step 1: Bootstrap ────────────────────────────────────────────────────
echo "═══════════════════════════════════════════════════════════════"
echo "  Step 1/3  Bootstrap Runtime Studio"
echo "═══════════════════════════════════════════════════════════════"
bash "${REPO_ROOT}/apps/runtime-studio/scripts/bootstrap_runtime_studio.sh"
echo

# ── Step 2: Scaffold + Optimize ──────────────────────────────────────────
DEFAULT_SPEC="${REPO_ROOT}/.nexo/workflow/workflow_lab.runtime.json"
RESOLVED_SPEC="${SPEC_PATH:-${DEFAULT_SPEC}}"

if [[ "${SKIP_OPTIMIZE}" -eq 0 ]]; then
  echo "═══════════════════════════════════════════════════════════════"
  echo "  Step 2/3  Optimize agent cluster for local hardware"
  echo "═══════════════════════════════════════════════════════════════"

  # Scaffold the spec file if it doesn't exist yet so optimize has input.
  if [[ ! -f "${RESOLVED_SPEC}" ]]; then
    echo "Scaffolding default workflow lab spec → ${RESOLVED_SPEC}"
    dotnet run --project "${REPO_ROOT}/src/Nexo.CLI" -- workflow scaffold \
      --output "${RESOLVED_SPEC}"
    echo
  fi

  OPTIMIZE_CMD=(
    dotnet run --project "${REPO_ROOT}/src/Nexo.CLI" -- workflow optimize
    --spec "${RESOLVED_SPEC}"
    --search-strategy "${SEARCH_STRATEGY}"
    --max-candidates "${MAX_CANDIDATES}"
    --auto-pull-models
    --promote-winner
    --persist-history
  )

  [[ -n "${OBJECTIVE}" ]]      && OPTIMIZE_CMD+=(--objective "${OBJECTIVE}")
  [[ -n "${OBJECTIVE_FILE}" ]] && OPTIMIZE_CMD+=(--objective-file "${OBJECTIVE_FILE}")
  [[ -n "${REPORT_OUTPUT}" ]]  && OPTIMIZE_CMD+=(--report-output "${REPORT_OUTPUT}")
  [[ -n "${PROVIDER}" ]]       && OPTIMIZE_CMD+=(--provider "${PROVIDER}")
  [[ -n "${PREFER}" ]]         && OPTIMIZE_CMD+=(--prefer "${PREFER}")
  [[ "${VERBOSE}" -eq 1 ]]     && OPTIMIZE_CMD+=(--verbose)

  echo "Running: ${OPTIMIZE_CMD[*]}"
  echo "  spec:             ${RESOLVED_SPEC}"
  echo "  search-strategy:  ${SEARCH_STRATEGY}"
  echo "  max-candidates:   ${MAX_CANDIDATES}"
  echo "  ollama:           ${OLLAMA_BASE_URL} (${OLLAMA_MODEL})"
  [[ -n "${OBJECTIVE}" ]]      && echo "  objective:        ${OBJECTIVE}"
  [[ -n "${OBJECTIVE_FILE}" ]] && echo "  objective-file:   ${OBJECTIVE_FILE}"
  [[ -n "${REPORT_OUTPUT}" ]]  && echo "  report-output:    ${REPORT_OUTPUT}"
  echo

  cd "${REPO_ROOT}"
  "${OPTIMIZE_CMD[@]}"
  OPTIMIZE_EXIT=$?

  echo
  if [[ ${OPTIMIZE_EXIT} -eq 0 ]]; then
    echo "Optimize completed successfully."
  else
    echo "Optimize finished with exit code ${OPTIMIZE_EXIT}." >&2
    echo "Review the output above for details. Continuing to daemon step (if enabled)."
  fi
  echo
else
  echo "═══════════════════════════════════════════════════════════════"
  echo "  Step 2/3  Optimize  [SKIPPED]"
  echo "═══════════════════════════════════════════════════════════════"
  echo
fi

# ── Step 3: Daemon ────────────────────────────────────────────────────────
if [[ "${SKIP_DAEMON}" -eq 0 ]]; then
  echo "═══════════════════════════════════════════════════════════════"
  echo "  Step 3/3  Start background agent daemon"
  echo "═══════════════════════════════════════════════════════════════"

  if [[ ! -f "${AGENT_SET_CONFIG}" ]]; then
    echo "Missing agent-set config: ${AGENT_SET_CONFIG}" >&2
    exit 1
  fi

  DAEMON_CMD=(
    dotnet run --project "${REPO_ROOT}/src/Nexo.CLI" -- background-agent daemon
    --config "${AGENT_SET_CONFIG}"
    --duration "${DURATION}"
  )

  [[ "${DISABLE_OBSERVATION}" -eq 1 ]] && DAEMON_CMD+=(--disable-observation)
  [[ "${FORMAT_JSON}" -eq 1 ]]         && DAEMON_CMD+=(--format-json)

  echo "Running agent daemon"
  echo "  config:      ${AGENT_SET_CONFIG}"
  echo "  duration:    ${DURATION}"
  echo "  observation: $([[ "${DISABLE_OBSERVATION}" -eq 1 ]] && echo "disabled" || echo "enabled")"
  echo "  ollama:      ${OLLAMA_BASE_URL} (${OLLAMA_MODEL})"
  echo

  cd "${REPO_ROOT}"
  "${DAEMON_CMD[@]}"
else
  echo "═══════════════════════════════════════════════════════════════"
  echo "  Step 3/3  Daemon  [SKIPPED]"
  echo "═══════════════════════════════════════════════════════════════"
  echo "  Pass --duration <dur> to start the agent daemon after optimization."
  echo
fi
