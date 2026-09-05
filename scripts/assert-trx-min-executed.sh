#!/usr/bin/env bash
# Locate python3 or python, then fail closed on a missing/empty TRX.
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if command -v python3 >/dev/null 2>&1; then
  exec python3 "${root}/assert-trx-min-executed.py" "$@"
fi
if command -v python >/dev/null 2>&1; then
  exec python "${root}/assert-trx-min-executed.py" "$@"
fi
echo "assert-trx-min-executed: python3/python not found" >&2
exit 1
