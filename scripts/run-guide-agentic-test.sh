#!/usr/bin/env bash
# Run the Universal Tester agent in agentic mode (real LLM) to test the Nexo Guide
# Avalonia app via clicks and keystrokes. Requires a display and an LLM (Ollama recommended).
#
# Runs:
#   - Guide_Agentic_UniversalTester_ShouldTestAllInteractions_WhenRunWithOllama (full UI test)
#   - Guide_AvaloniaApp_UniversalTester_ShouldPerformClicksAndKeystrokes_WhenTestingWithDesktopAdapter (clicks/type assertions)
#
# Prerequisites:
#   - Ollama running (e.g. ollama serve) with a vision model for understanding (e.g. ollama pull llava:7b),
#     or use scripts/run-guide-agentic-test-docker.sh to run Ollama via Docker
#   - Display available (do not set NEXO_SKIP_UI_TESTS=1)
#   - For Docker Ollama, set OLLAMA_MODEL / OLLAMA_VISION_MODEL to match your image
#
# Usage:
#   ./scripts/run-guide-agentic-test.sh
#   ./scripts/run-guide-agentic-test-docker.sh   # Ollama via Docker
#
set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

export NEXO_RUN_AGENTIC_GUIDE_TEST=1
unset NEXO_SKIP_UI_TESTS

echo "=== Agentic Guide tests (Universal Tester + LLM, Avalonia UI) ==="
echo "Provider: ollama (vision for understanding)"
echo ""

dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -f net8.0 \
  --filter "FullyQualifiedName~Guide_Agentic_UniversalTester_ShouldTestAllInteractions|FullyQualifiedName~Guide_AvaloniaApp_UniversalTester_ShouldPerformClicksAndKeystrokes" \
  -p:TreatWarningsAsErrors=false \
  --logger "console;verbosity=normal"
