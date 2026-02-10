#!/usr/bin/env bash
# Build and run Nexo.Guide in the iOS Simulator (recommended on macOS 26 where Catalyst crashes).
# Requires .NET 9 SDK and Xcode. Start an iOS simulator first (Xcode → Open Developer Tool → Simulator).

set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

dotnet run --project src/Nexo.Guide/Nexo.Guide.csproj -f net9.0-ios -p:TreatWarningsAsErrors=false "$@"
