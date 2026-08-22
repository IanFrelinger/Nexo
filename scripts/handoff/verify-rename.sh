#!/usr/bin/env bash
# Assert the Nexo -> Ashlar rename left nothing behind.
#
#   bash scripts/handoff/verify-rename.sh
#
# Exit 0 = clean. Exit 1 = residue, listed. Run this before you build: a stray
# token in a .csproj or a config key fails at runtime, not at compile time, and
# is much cheaper to find here.

set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

FAIL=0

echo "=== verifying rename Nexo -> Ashlar ==="
echo

# ---- 1. content residue -------------------------------------------------------
# _handoff/ is excluded on purpose: the extracted game layer keeps its own name
# and consumes Ashlar as a package.
echo "--- residual tokens in file content ---"
HITS="$(grep -rIn -e 'Nexo' -e 'NEXO' -e 'nexo' . \
        --exclude-dir=.git --exclude-dir=_handoff 2>/dev/null || true)"
if [[ -n "$HITS" ]]; then
  echo "$HITS" | head -40
  COUNT="$(printf '%s\n' "$HITS" | wc -l | tr -d ' ')"
  echo "... $COUNT line(s) still contain a Nexo token"
  FAIL=1
else
  echo "clean"
fi
echo

# ---- 2. path residue ----------------------------------------------------------
echo "--- residual Nexo in file and directory names ---"
PATHS="$(find . -iname '*nexo*' -not -path './.git/*' -not -path './_handoff/*' 2>/dev/null || true)"
if [[ -n "$PATHS" ]]; then
  echo "$PATHS" | head -40
  echo "... $(printf '%s\n' "$PATHS" | wc -l | tr -d ' ') path(s) still contain 'nexo'"
  FAIL=1
else
  echo "clean"
fi
echo

# ---- 3. solution integrity ----------------------------------------------------
# Every project path named by a .sln / .slnf must exist. This is the check that
# catches a directory rename that ran but left a stale reference behind.
echo "--- solution project references resolve ---"
MISSING=0
for sln in *.sln application/*.sln; do
  [[ -f "$sln" ]] || continue
  while IFS= read -r proj; do
    proj="${proj//\\//}"
    [[ -f "$proj" ]] || { echo "  MISSING: $proj  (referenced by $sln)"; MISSING=$((MISSING+1)); }
  done < <(grep -oE '"[^"]+\.csproj"' "$sln" 2>/dev/null | tr -d '"' || true)
done
for slnf in *.slnf; do
  [[ -f "$slnf" ]] || continue
  while IFS= read -r proj; do
    proj="${proj//\\//}"
    [[ -f "$proj" ]] || { echo "  MISSING: $proj  (referenced by $slnf)"; MISSING=$((MISSING+1)); }
  done < <(grep -oE '"[^"]+\.csproj"' "$slnf" 2>/dev/null | tr -d '"' || true)
done
if [[ $MISSING -gt 0 ]]; then echo "$MISSING missing project path(s)"; FAIL=1; else echo "clean"; fi
echo

# ---- 4. ProjectReference integrity -------------------------------------------
echo "--- ProjectReference paths resolve ---"
BADREF=0
while IFS= read -r csproj; do
  d="$(dirname "$csproj")"
  while IFS= read -r ref; do
    ref="${ref//\\//}"
    [[ -f "$d/$ref" ]] || { echo "  MISSING: $ref  (from $csproj)"; BADREF=$((BADREF+1)); }
  done < <(grep -oE 'Include="[^"]+\.csproj"' "$csproj" 2>/dev/null | sed 's/Include="//;s/"$//' || true)
done < <(find . -name '*.csproj' -not -path './.git/*' -not -path './_handoff/*')
if [[ $BADREF -gt 0 ]]; then echo "$BADREF broken ProjectReference(s)"; FAIL=1; else echo "clean"; fi
echo

# ---- verdict ------------------------------------------------------------------
if [[ $FAIL -eq 0 ]]; then
  echo "PASS — no residue. Safe to build:  dotnet build Ashlar.Kernel.sln"
  exit 0
fi
echo "FAIL — fix the above before building."
exit 1
