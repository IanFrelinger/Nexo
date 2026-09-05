#!/usr/bin/env bash
# Fail closed when a `dotnet test` log shows zero executed tests.
# `dotnet test --filter` exits 0 when discovery matches nothing.
set -euo pipefail

log="${1:-}"
if [[ -z "${log}" || ! -f "${log}" ]]; then
  echo "assert-dotnet-test-executed: missing log" >&2
  exit 1
fi

if grep -qiE 'No test is available|No test matches' "${log}"; then
  echo "assert-dotnet-test-executed: empty match in ${log}" >&2
  exit 1
fi

passed="$(grep -oE 'Passed:[[:space:]]+[0-9]+' "${log}" | tail -1 | grep -oE '[0-9]+' || true)"
if [[ "${passed:-0}" -lt 1 ]]; then
  echo "assert-dotnet-test-executed: Passed=${passed:-0} in ${log}" >&2
  exit 1
fi

exit 0
