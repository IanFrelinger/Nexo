#!/usr/bin/env bash
set -euo pipefail
d="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec env NEXO_INSTALL_PLATFORM=darwin bash "${d}/install_common.sh" "$@"
