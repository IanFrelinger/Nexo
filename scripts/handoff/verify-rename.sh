#!/usr/bin/env bash
# Assert the Nexo -> Ashlar rename left nothing behind.
#
#   bash scripts/handoff/verify-rename.sh
#
# Exit 0 = clean. Exit 1 = residue, listed. Run this before you build: a stray
# token in a .csproj or a config key fails at runtime, not at compile time, and
# is much cheaper to find here.
#
# SCOPE MATCHES rename-to-ashlar.sh: tracked files only, via `git ls-files`.
#
# This is not cosmetic. An earlier revision scanned with `grep -r .` and `find`,
# excluding only .git and _handoff. Because the rename itself is correctly scoped
# to tracked files, those scans see leftover `Nexo` tokens in build output
# (bin/, obj/) and in any git worktree parked inside the tree — and report FAIL
# on a rename that was in fact entirely correct. Untracked residue is expected and is
# reported below as INFO, never as failure.

set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

git rev-parse --git-dir >/dev/null 2>&1 || { echo "not a git repository: $REPO_ROOT" >&2; exit 1; }

FAIL=0

# SCOPE MUST MATCH rename-to-ashlar.sh EXACTLY. If the verifier checks files the
# rename deliberately skipped, it reports them as residue and fails a correct run —
# which is what happened: 83 lines across docs/handoff/ and scripts/handoff/ were
# flagged, every one of them a deliberate exclusion.
#
#   _handoff/         the extracted game layer keeps its own name
#   scripts/handoff/  the rename's own tooling and documentation. Their Nexo
#   docs/handoff/     tokens are the definition of the work, not instances of it.
tracked() {
  git ls-files | grep -v -e "^_handoff/" -e "^scripts/handoff/" -e "^docs/handoff/"
}

echo "=== verifying rename Nexo -> Ashlar ==="
echo

# ---- 1. content residue -------------------------------------------------------
echo "--- residual tokens in tracked file content ---"
HITS="$(tracked | tr "\n" "\0" | xargs -0 grep -In -e "Nexo" -e "NEXO" -e "nexo" 2>/dev/null || true)"
if [[ -n "$HITS" ]]; then
  printf "%s\n" "$HITS" | head -40
  COUNT="$(printf "%s\n" "$HITS" | wc -l | tr -d " ")"
  echo "... $COUNT line(s) still contain a Nexo token"
  FAIL=1
else
  echo "clean"
fi
echo

# ---- 2. path residue ----------------------------------------------------------
echo "--- residual Nexo in tracked file and directory names ---"
PATHS="$(tracked | grep -i "nexo" || true)"
if [[ -n "$PATHS" ]]; then
  printf "%s\n" "$PATHS" | head -40
  echo "... $(printf "%s\n" "$PATHS" | wc -l | tr -d " ") tracked path(s) still contain 'nexo'"
  FAIL=1
else
  echo "clean"
fi
echo

# ---- 3. solution integrity ----------------------------------------------------
# Every project path named by a .sln / .slnf must exist. This is the check that
# catches a directory rename that ran but left a stale reference behind.
#
# Paths inside a solution are relative to THAT SOLUTION'S directory, not to the
# repo root. Resolving them from the root reports 3 phantom MISSING entries for
# application/Nexo.Application.sln on a completely untouched tree.
echo "--- solution project references resolve ---"
MISSING=0
while IFS= read -r sln; do
  [[ -f "$sln" ]] || continue
  slndir="$(dirname "$sln")"
  while IFS= read -r proj; do
    proj="${proj//\\//}"
    [[ -f "$slndir/$proj" ]] || { echo "  MISSING: $slndir/$proj  (referenced by $sln)"; MISSING=$((MISSING+1)); }
  done < <(grep -oE '"[^"]+\.csproj"' "$sln" 2>/dev/null | tr -d '"' || true)
done < <(tracked | grep -E "\.slnf?$")
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
done < <(tracked | grep -E "\.csproj$")
if [[ $BADREF -gt 0 ]]; then echo "$BADREF broken ProjectReference(s)"; FAIL=1; else echo "clean"; fi
echo

# ---- INFO: untracked residue (never a failure) --------------------------------
echo "--- untracked residue (informational) ---"
UNTRACKED_HITS="$(git status --porcelain --ignored=matching 2>/dev/null \
                  | grep -E "^(\?\?|!!)" | sed "s/^...//" | grep -ic "nexo" || true)"
if [[ "${UNTRACKED_HITS:-0}" -gt 0 ]]; then
  echo "  $UNTRACKED_HITS untracked/ignored path(s) still contain 'nexo'."
  echo "  Expected. These are build output and any parked git worktree, both of"
  echo "  which are deliberately outside the rename's scope. To clear build output:"
  echo "    git clean -xdf -e .claude -- src application commercial applications"
else
  echo "  none"
fi
echo

# ---- verdict ------------------------------------------------------------------
if [[ $FAIL -eq 0 ]]; then
  echo "PASS — no residue in tracked files. Safe to build:  dotnet build Ashlar.Kernel.sln"
  exit 0
fi
echo "FAIL — fix the above before building."
exit 1
