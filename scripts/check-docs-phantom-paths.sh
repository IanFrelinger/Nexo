#!/usr/bin/env bash
# Check for phantom backtick-wrapped paths in docs before CI runs.
# Mirrors the logic from .github/workflows/onboarding-docs-guard.yml
# "Referenced repo paths must exist" step.
#
# Usage:
#   bash scripts/check-docs-phantom-paths.sh
#
# Exit codes:
#   0 - all referenced paths exist
#   1 - one or more referenced paths are missing

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "== Docs Phantom Path Check =="
echo "Scanning README.md, docs/, scripts/, and Makefile for backtick-wrapped repo paths..."

files=(README.md Makefile)
while IFS= read -r f; do files+=("$f"); done < <(find docs -type f -name '*.md' 2>/dev/null | sort)
while IFS= read -r f; do 
  # Exclude this script itself to avoid matching example paths in comments
  if [[ "$f" != "scripts/check-docs-phantom-paths.sh" ]]; then
    files+=("$f")
  fi
done < <(find scripts -maxdepth 1 -type f \( -name '*.sh' -o -name '*.ps1' \) 2>/dev/null | sort)

# Pattern matches backtick-wrapped paths
pattern='`(scripts|docs|deploy|\.github|src|application|applications|samples)/[A-Za-z0-9._/-]+\.(sh|ps1|md|yml|yaml|json|csproj|sln|slnf)`'
missing=0

while IFS= read -r token; do
  path="${token#\`}"; path="${path%\`}"
  
  # Placeholders are not real paths
  case "$path" in *'<'*|*'{'*) continue ;; esac
  
  # A path git is configured to ignore cannot exist in a clean checkout
  if git check-ignore -q -- "$path" 2>/dev/null; then
    continue
  fi
  
  if [ ! -e "$path" ]; then
    echo "ERROR: referenced path does not exist: $path"
    grep -lF -- "$token" "${files[@]}" 2>/dev/null | sed 's/^/    referenced by: /' || true
    missing=$((missing + 1))
  fi
done < <(grep -ohE -- "$pattern" "${files[@]}" 2>/dev/null | sort -u)

echo ""
if [ "$missing" -eq 0 ]; then
  echo "✓ All referenced paths exist (0 missing)"
  exit 0
else
  echo "✗ Found $missing missing path(s)"
  echo ""
  echo "Fix by either:"
  echo "  1. Creating the missing file/directory"
  echo "  2. Updating the documentation to reference the correct path"
  echo "  3. Adding the path to .gitignore if it's meant to be created by users"
  exit 1
fi
