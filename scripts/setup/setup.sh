#!/usr/bin/env bash
# macOS/Linux: delegates to setup-unix.sh (bash parity with setup.ps1 on Windows).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec "${SCRIPT_DIR}/setup-unix.sh" "$@"
