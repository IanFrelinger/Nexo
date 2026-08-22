#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
LOG_PATH="${REPO_ROOT}/.artifacts/install_bruteforce_matrix.log"
SUMMARY_PATH="${REPO_ROOT}/.artifacts/install_bruteforce_summary.log"

mkdir -p "$(dirname "${LOG_PATH}")"
: > "${LOG_PATH}"
: > "${SUMMARY_PATH}"

TOTAL_CASES=0
PASSED_CASES=0
FAILED_CASES=0

run_case() {
  local name="$1"
  local expected="$2"
  local command="$3"

  TOTAL_CASES=$((TOTAL_CASES + 1))

  {
    echo "===== CASE: ${name} ====="
    echo "CMD: ${command}"
  } | tee -a "${LOG_PATH}"

  set +e
  bash -lc "${command}" >> "${LOG_PATH}" 2>&1
  local exit_code=$?
  set -e

  echo "EXIT: ${exit_code}" | tee -a "${LOG_PATH}"

  if [[ "${expected}" == "pass" && ${exit_code} -eq 0 ]]; then
    PASSED_CASES=$((PASSED_CASES + 1))
    echo "RESULT: OK" | tee -a "${LOG_PATH}"
    echo | tee -a "${LOG_PATH}"
    return 0
  fi

  if [[ "${expected}" == "fail" && ${exit_code} -ne 0 ]]; then
    PASSED_CASES=$((PASSED_CASES + 1))
    echo "RESULT: OK" | tee -a "${LOG_PATH}"
    echo | tee -a "${LOG_PATH}"
    return 0
  fi

  FAILED_CASES=$((FAILED_CASES + 1))
  if [[ "${expected}" == "pass" ]]; then
    echo "RESULT: FAIL (expected pass)" | tee -a "${LOG_PATH}"
  else
    echo "RESULT: FAIL (expected failure)" | tee -a "${LOG_PATH}"
  fi
  echo | tee -a "${LOG_PATH}"
  return 1
}

run_case "bash syntax setup + container bootstrap scripts" "pass" \
  "bash -n ${REPO_ROOT}/scripts/setup/setup-linux.sh && \
   bash -n ${REPO_ROOT}/scripts/setup/setup-macos.sh && \
   bash -n ${REPO_ROOT}/scripts/install/container-bootstrap-linux.sh && \
   bash -n ${REPO_ROOT}/scripts/install/container-bootstrap-macos.sh"

run_case "setup-linux help" "pass" \
  "bash ${REPO_ROOT}/scripts/setup/setup-linux.sh --help"

run_case "setup-linux unknown mode" "fail" \
  "bash ${REPO_ROOT}/scripts/setup/setup-linux.sh banana"

run_case "setup-linux noninteractive no-yes missing deps path" "fail" \
  "tmp_path=\"\$(mktemp -d)\" && \
   ln -s /usr/bin/apt-get \"\${tmp_path}/apt-get\" && \
   PATH=\"\${tmp_path}\" env -i PATH=\"\${tmp_path}\" HOME=\$HOME /usr/bin/bash ${REPO_ROOT}/scripts/setup/setup-linux.sh apply"

run_case "setup-linux guided apply with yes" "pass" \
  "bash ${REPO_ROOT}/scripts/setup/setup-linux.sh apply --yes --guided"

run_case "container-bootstrap-linux dry-run" "pass" \
  "bash ${REPO_ROOT}/scripts/install/container-bootstrap-linux.sh --dry-run --yes"

run_case "container-bootstrap-linux guided dry-run workspace" "pass" \
  "bash ${REPO_ROOT}/scripts/install/container-bootstrap-linux.sh --dry-run --yes --guided --workspace ${REPO_ROOT}"

run_case "container-bootstrap-linux start-daemon dry-run" "pass" \
  "bash ${REPO_ROOT}/scripts/install/container-bootstrap-linux.sh --dry-run --yes --workspace ${REPO_ROOT} --start-daemon 20s | grep -q 'background-agent daemon --duration'"

run_case "container dispatcher unsupported os via uname override" "fail" \
  "uname(){ echo FreeBSD; }; export -f uname; ${REPO_ROOT}/scripts/install/container-bootstrap.sh --dry-run"

run_case "dotnet restore CLI project" "pass" \
  "dotnet restore ${REPO_ROOT}/application/src/Ashlar.CLI/Ashlar.CLI.csproj"

run_case "dotnet restore copy-assemblies helper project" "pass" \
  "dotnet restore ${REPO_ROOT}/src/Ashlar.Tests.Infrastructure/scripts/copy-assemblies.csproj"

run_case "dotnet build CLI project" "pass" \
  "dotnet build ${REPO_ROOT}/application/src/Ashlar.CLI/Ashlar.CLI.csproj --no-restore -v minimal"

{
  echo "===== SUMMARY ====="
  echo "total_cases=${TOTAL_CASES}"
  echo "passed_cases=${PASSED_CASES}"
  echo "failed_cases=${FAILED_CASES}"
  echo "log_path=${LOG_PATH}"
} | tee -a "${LOG_PATH}" | tee "${SUMMARY_PATH}"

if [[ ${FAILED_CASES} -ne 0 ]]; then
  exit 1
fi
