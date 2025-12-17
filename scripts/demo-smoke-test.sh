#!/usr/bin/env bash
set -euo pipefail

# Demo Smoke Test - Validates demo scripts for portability
# Checks for hardcoded paths and validates UNITY_BIN support

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "🔍 Demo Script Smoke Test"
echo "========================"
echo ""

# Find all Unity demo scripts
UNITY_SCRIPTS=(
  "scripts/unity-configurable-spec-wizard.sh"
  "scripts/unity-game-spec-wizard.sh"
  "scripts/unity-interactive-spec-wizard.sh"
  "scripts/unity-playmode-fallback.sh"
  "scripts/unity-playtest-integration.sh"
  "scripts/unity-playtest-run.sh"
  "scripts/unity-real-scene-smoke.sh"
  "scripts/unity-simple-playtest.sh"
  "scripts/unity-complete-workflow.sh"
  "scripts/unity-spec-driven-generator.sh"
  "scripts/run-custom-demo.sh"
  "scripts/run-for-review.sh"
)

ERRORS=0
WARNINGS=0
UNITY_BIN_COUNT=0

echo "Checking for hardcoded user paths..."
echo ""

for script in "${UNITY_SCRIPTS[@]}"; do
  script_path="$ART/$script"
  
  if [[ ! -f "$script_path" ]]; then
    echo "⚠️  WARNING: Script not found: $script"
    ((WARNINGS++))
    continue
  fi
  
  # Check for hardcoded user paths
  if grep -q "/Users/ianfrelinger" "$script_path" 2>/dev/null; then
    echo "❌ FAIL: $script contains hardcoded user path"
    grep -n "/Users/ianfrelinger" "$script_path" | head -3
    ((ERRORS++))
  else
    echo "✅ PASS: $script - No hardcoded user paths"
  fi
  
  # Check for UNITY_BIN support
  if grep -q 'UNITY_BIN' "$script_path" 2>/dev/null || grep -q '\${UNITY_BIN' "$script_path" 2>/dev/null; then
    ((UNITY_BIN_COUNT++))
  fi
done

echo ""
echo "Checking for UNITY_BIN environment variable support..."
echo "Found $UNITY_BIN_COUNT scripts with UNITY_BIN support"
echo ""

# Check for SCRIPT_DIR pattern
echo "Checking for portable path detection pattern..."
SCRIPT_DIR_COUNT=0
for script in "${UNITY_SCRIPTS[@]}"; do
  script_path="$ART/$script"
  if [[ -f "$script_path" ]] && grep -q 'SCRIPT_DIR=' "$script_path" 2>/dev/null; then
    ((SCRIPT_DIR_COUNT++))
  fi
done

echo "Found $SCRIPT_DIR_COUNT scripts using SCRIPT_DIR pattern"
echo ""

# Summary
echo "========================"
echo "Summary:"
echo "  Scripts checked: ${#UNITY_SCRIPTS[@]}"
echo "  Scripts with UNITY_BIN support: $UNITY_BIN_COUNT"
echo "  Scripts with SCRIPT_DIR pattern: $SCRIPT_DIR_COUNT"
echo "  Errors (hardcoded paths): $ERRORS"
echo "  Warnings: $WARNINGS"
echo ""

if [[ $ERRORS -eq 0 ]]; then
  echo "✅ SUCCESS: No hardcoded user paths found"
  exit 0
else
  echo "❌ FAILURE: Found $ERRORS script(s) with hardcoded paths"
  exit 1
fi

