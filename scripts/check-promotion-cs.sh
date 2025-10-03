#!/usr/bin/env bash
set -euo pipefail

# C# version of promotion checker
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
UTIL_PATH="$PROJECT_ROOT/src/ValidationUtilities"

# Build if needed
if [[ ! -f "$UTIL_PATH/bin/Debug/net8.0/ValidationUtilities" ]]; then
    echo "Building ValidationUtilities..."
    cd "$UTIL_PATH"
    dotnet build
    cd "$PROJECT_ROOT"
fi

# Run the C# utility
exec "$UTIL_PATH/bin/Debug/net8.0/ValidationUtilities" check-promotion "$@"
