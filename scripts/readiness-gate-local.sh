#!/usr/bin/env bash
# Local readiness gate for the agent convergence loop.
# Runs the same tier scripts CI's application-gate workflow runs (its "full"
# dispatch lane = tiers A+B+C with tier D skipped and ASHLAR_ALLOW_MOCK=1 /
# APPLICATION_GATE_SKIP_KERNEL=1, per .github/workflows/application-gate.yml),
# plus the complete Ashlar.Tests.CLI suite as a strictly stronger local gate
# (CI tier B only samples three command-test classes).
# --layer applications mirrors provenance-graph-gate.yml + dependency-boundary.yml
# and adds the full Ashlar.Applications.Tests suite and a per-project build.
# Emits RESULT lines (e2e-loop.sh convention) and, with --json, a
# machine-readable verdict for orchestrating agents.
# Canonical environment: the dotnet 10 dev container / CI runner. Works under
# Git Bash on Windows, but container results are the ones that count.
# Usage: bash scripts/readiness-gate-local.sh [--layer application] [--json out.json] [--include-tier-d]
set -uo pipefail # no -e: every gate runs; failures are counted, not fatal (see e2e-loop.sh)

CALLER_PWD="$PWD"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

LAYER="application"
JSON_OUT=""
INCLUDE_TIER_D=0
while [ $# -gt 0 ]; do
  case "$1" in
    --layer) LAYER="${2:?--layer needs a value}"; shift 2 ;;
    --json) JSON_OUT="${2:?--json needs a value}"; shift 2 ;;
    --include-tier-d) INCLUDE_TIER_D=1; shift ;;
    *) echo "readiness-gate-local: unknown argument: $1" >&2; exit 64 ;;
  esac
done

# A relative --json path is the caller's, not the repo root's (we cd'd above).
case "$JSON_OUT" in
  ''|/*) ;;
  *) JSON_OUT="$CALLER_PWD/$JSON_OUT" ;;
esac

case "$LAYER" in
  application)
    GATE_NAMES=(application-tier-a application-tier-b application-tier-c application-tests-cli-full)
    GATE_CMDS=(
      "ASHLAR_ALLOW_MOCK=1 APPLICATION_GATE_SKIP_KERNEL=1 bash scripts/application-gate-tier-a.sh"
      "ASHLAR_ALLOW_MOCK=1 bash scripts/application-gate-tier-b.sh"
      "ASHLAR_ALLOW_MOCK=1 bash scripts/application-gate-tier-c.sh"
      "dotnet build application/src/Ashlar.Tests.CLI/Ashlar.Tests.CLI.csproj -v minimal && ASHLAR_ALLOW_MOCK=1 dotnet test application/src/Ashlar.Tests.CLI/Ashlar.Tests.CLI.csproj -f net10.0 --no-build --blame-hang-timeout 120s --blame-hang-dump-type none"
    )
    if [ "$INCLUDE_TIER_D" = "1" ]; then
      GATE_NAMES+=(application-tier-d)
      GATE_CMDS+=("bash scripts/application-gate-tier-d.sh")
    fi
    ;;
  applications)
    # Mirrors CI for this layer: provenance-graph-gate.yml's unit lane and
    # dependency-boundary.yml (paths include applications/**), plus strictly
    # stronger local coverage CI lacks: a per-project build of every
    # applications/ csproj and the full Ashlar.Applications.Tests suite (no CI
    # workflow runs it). The provenance Neo4j integration lane
    # (Testcontainers, needs a Docker daemon) rides behind --include-tier-d
    # like application's tier D. Test projects target net8.0 — the container
    # needs the .NET 8 runtime alongside SDK 10 (CI installs 10.0.x + 8.0.x).
    GATE_NAMES=(applications-build applications-tests-full applications-provenance-unit applications-dependency-boundary applications-coverage)
    GATE_CMDS=(
      'for p in applications/*/*.csproj; do dotnet build "$p" --configuration Release -v minimal || exit 1; done'
      "dotnet test applications/Ashlar.Applications.Tests/Ashlar.Applications.Tests.csproj --configuration Release --no-build --blame-hang-timeout 120s --blame-hang-dump-type none"
      'dotnet test applications/Ashlar.Provenance.Graph.Tests/Ashlar.Provenance.Graph.Tests.csproj --filter "Category!=Integration" --configuration Release'
      "bash scripts/dependency-boundary-gate.sh"
      "bash scripts/applications-coverage-gate.sh"
    )
    if [ "$INCLUDE_TIER_D" = "1" ]; then
      # Under docker-outside-of-docker the test process cannot reach Ryuk (its
      # port publishes on the host daemon, not this container's localhost), so
      # the reaper is disabled — the fixture disposes its own container — and
      # published ports are reached via the host gateway.
      GATE_NAMES+=(applications-provenance-integration)
      GATE_CMDS+=('ASHLAR_RUN_NEO4J_CONTAINER=1 TESTCONTAINERS_RYUK_DISABLED=true TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal dotnet test applications/Ashlar.Provenance.Graph.Tests/Ashlar.Provenance.Graph.Tests.csproj --filter "Category=Integration" --configuration Release')
    fi
    ;;
  apps)
    # Mirrors CI for this layer: optimize-agent-cluster-gate.yml is the ONLY
    # CI workflow owning apps/ paths (it exercises apps/runtime-studio's
    # optimize_agent_cluster.sh). scripts/apps-gate-checks.sh reproduces its
    # five jobs. ashlar-forge, game-director and release-manager have no CI
    # coverage and no csproj — what "ready" means for them is parked in the
    # ledger as a product question, not guessed here.
    GATE_NAMES=(apps-cli-build apps-script-interface apps-bootstrap apps-scaffold-optimize apps-daemon-launch apps-flag-combinations)
    GATE_CMDS=(
      "dotnet restore src/Ashlar.Tests.Infrastructure/scripts/copy-assemblies.csproj && dotnet build application/src/Ashlar.CLI/Ashlar.CLI.csproj -v minimal"
      "bash scripts/apps-gate-checks.sh interface"
      "bash scripts/apps-gate-checks.sh bootstrap"
      "bash scripts/apps-gate-checks.sh scaffold"
      "bash scripts/apps-gate-checks.sh daemon"
      "bash scripts/apps-gate-checks.sh combos"
    )
    ;;
  *)
    echo "readiness-gate-local: unknown layer: $LAYER" >&2
    exit 64
    ;;
esac

LOG_DIR="${RUNNER_TEMP:-${TMPDIR:-/tmp}}/readiness-gate-$$"
mkdir -p "$LOG_DIR"
# "unknown" inside the dev container: a linked worktree's gitdir points at the host.
COMMIT="$(git rev-parse HEAD 2>/dev/null || echo unknown)"
STARTED_AT="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

FAILED=0
declare -a STATUSES DURATIONS CODES

for i in "${!GATE_NAMES[@]}"; do
  name="${GATE_NAMES[$i]}"
  log="$LOG_DIR/$name.log"
  echo "== $name =="
  start=$(date +%s)
  bash -c "${GATE_CMDS[$i]}" >"$log" 2>&1
  code=$?
  dur=$(( $(date +%s) - start ))
  if [ "$code" -eq 0 ]; then status=PASS; else status=FAIL; FAILED=$((FAILED + 1)); fi
  STATUSES[$i]=$status
  DURATIONS[$i]=$dur
  CODES[$i]=$code
  printf 'RESULT\t%d\t%s\t%s\texit=%d dur=%ds log=%s\n' "$((i + 1))" "$name" "$status" "$code" "$dur" "$log"
  if [ "$status" = FAIL ]; then
    echo "-- last 40 log lines ($name) --"
    tail -n 40 "$log"
  fi
done

if [ -n "$JSON_OUT" ]; then
  if command -v jq >/dev/null 2>&1; then
    : > "$LOG_DIR/entries.jsonl"
    for i in "${!GATE_NAMES[@]}"; do
      tail -n 60 "$LOG_DIR/${GATE_NAMES[$i]}.log" > "$LOG_DIR/tail.txt"
      jq -n \
        --arg gate "${GATE_NAMES[$i]}" \
        --arg status "${STATUSES[$i]}" \
        --argjson exit "${CODES[$i]}" \
        --argjson dur "${DURATIONS[$i]}" \
        --arg log "$LOG_DIR/${GATE_NAMES[$i]}.log" \
        --rawfile tail "$LOG_DIR/tail.txt" \
        '{gate: $gate, status: $status, exit_code: $exit, duration_s: $dur, log: $log, log_tail: (if $status == "FAIL" then $tail else "" end)}' \
        >> "$LOG_DIR/entries.jsonl"
    done
    if jq -s \
      --arg layer "$LAYER" \
      --arg commit "$COMMIT" \
      --arg started "$STARTED_AT" \
      --argjson failed "$FAILED" \
      '{layer: $layer, commit: $commit, started_at: $started, failed: $failed, gates: .}' \
      "$LOG_DIR/entries.jsonl" > "$JSON_OUT"; then
      echo "json: $JSON_OUT"
    else
      echo "readiness-gate-local: failed to write $JSON_OUT" >&2
      FAILED=$((FAILED + 1))
    fi
  else
    echo "readiness-gate-local: jq not found; cannot write requested --json output" >&2
    FAILED=$((FAILED + 1))
  fi
fi

echo "==================== readiness-gate verdict ===================="
echo "layer: $LAYER commit: $COMMIT gates: ${#GATE_NAMES[@]} failed: $FAILED"
if [ "$FAILED" -eq 0 ]; then
  echo "readiness-gate-local[$LAYER]: PASS"
  exit 0
fi
echo "readiness-gate-local[$LAYER]: FAIL"
exit 1
