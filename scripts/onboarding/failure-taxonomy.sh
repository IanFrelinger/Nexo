#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 2 ]]; then
  echo "usage: $0 <check-name> <log-file>" >&2
  exit 1
fi

CHECK_NAME="$1"
LOG_FILE="$2"

if [[ ! -f "$LOG_FILE" ]]; then
  CHECK_NAME="$CHECK_NAME" python - <<'PY'
import json
import os
print(json.dumps({
    "check": os.environ["CHECK_NAME"],
    "category": "missing-log",
    "rootCauseClass": "artifact-missing",
    "remediation": "Check workflow step wiring and artifact upload paths."
}))
PY
  exit 0
fi

normalized="$(tr '[:upper:]' '[:lower:]' < "$LOG_FILE")"
category="unknown"
root_cause="unknown"
remediation="Review log details and map to onboarding runbook."

if grep -q "command not found" <<< "$normalized"; then
  category="missing-dependency"
  root_cause="tooling-missing"
  remediation="Run doctor --fix --yes or install required tools listed in onboarding docs."
elif grep -q "permission denied" <<< "$normalized"; then
  category="host-permissions"
  root_cause="permissions"
  remediation="Grant execute permissions or run with a user that has required access."
elif grep -q "network is unreachable" <<< "$normalized" || grep -q "could not resolve host" <<< "$normalized"; then
  category="network"
  root_cause="connectivity"
  remediation="Verify DNS/network egress and retry."
elif grep -q "nuget" <<< "$normalized" || grep -q "restore failed" <<< "$normalized"; then
  category="dotnet-restore"
  root_cause="package-restore"
  remediation="Retry restore; verify NuGet feeds and credentials."
elif grep -q "docker" <<< "$normalized" && grep -q "cannot connect" <<< "$normalized"; then
  category="docker-daemon"
  root_cause="container-runtime"
  remediation="Start Docker daemon/service before rerunning onboarding gate."
fi

CHECK_NAME="$CHECK_NAME" CATEGORY="$category" ROOT_CAUSE="$root_cause" REMEDIATION="$remediation" python - <<'PY'
import json
import os
print(json.dumps({
    "check": os.environ["CHECK_NAME"],
    "category": os.environ["CATEGORY"],
    "rootCauseClass": os.environ["ROOT_CAUSE"],
    "remediation": os.environ["REMEDIATION"]
}))
PY
