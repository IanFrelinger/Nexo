#!/usr/bin/env bash
# Build and run Nexo.Guide on Mac Catalyst (requires .NET 9 SDK).
# Use -p:TreatWarningsAsErrors=false so dependency projects (e.g. Nexo.Abstractions) don't fail the build.
#
# Note: On macOS 26.x this app often crashes at launch (Swift Observation in UIKit/Catalyst).
# Use the iOS Simulator instead: ./scripts/run-guide-ios-simulator.sh

set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

dotnet run --project src/Nexo.Guide/Nexo.Guide.csproj -f net9.0-maccatalyst -p:TreatWarningsAsErrors=false "$@"
