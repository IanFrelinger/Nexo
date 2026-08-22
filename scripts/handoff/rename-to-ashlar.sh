#!/usr/bin/env bash
# Rename Nexo -> Ashlar across the whole repository.
#
# Dry run by default. Nothing is written unless you pass --apply.
#
#   bash scripts/handoff/rename-to-ashlar.sh            # report what would change
#   bash scripts/handoff/rename-to-ashlar.sh --apply    # do it
#
# The script is IDEMPOTENT: running it twice is a no-op, because after the first
# pass there are no `Nexo` tokens left to match.
#
# Three ordered passes, and the order matters:
#   1. file CONTENT   (namespaces, types, config keys, docs, env vars)
#   2. FILE names     (Nexo.Core.Domain.csproj -> Ashlar.Core.Domain.csproj)
#   3. DIRECTORY names, deepest first (a parent rename would invalidate child paths)
#
# Token map — all three cases are real in this repo and mean different things:
#   Nexo   -> Ashlar    PascalCase: namespaces, type names, assembly + package ids
#   NEXO   -> ASHLAR    SCREAMING: environment variables (NEXO_ALLOW_MOCK, ...)
#   nexo   -> ashlar    lowercase: CLI binary, .nexo/ state dir, ghcr image paths
#
# Verified before writing this script: no natural-English word in the tree contains
# "nexo" as a substring (checked with `grep -oiE '[a-z]nexo|nexo[a-z]'` — every hit
# is a compound identifier such as AddNexo, INexoClient, UseNexoGovernance). A plain
# token replacement is therefore safe and needs no word-boundary guard.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

APPLY=0
[[ "${1:-}" == "--apply" ]] && APPLY=1

if [[ $APPLY -eq 0 ]]; then
  echo "=== DRY RUN — nothing will be written. Pass --apply to execute. ==="
else
  echo "=== APPLYING rename Nexo -> Ashlar in $REPO_ROOT ==="
fi
echo

# Paths never touched:
#   .git/       — repository metadata
#   _handoff/   — the extracted game layer keeps its own identity (see extract-game-layer.sh)
#   binary assets — matched by extension below
PRUNE=( -path ./.git -o -path ./_handoff )

is_text() {
  # Skip anything git considers binary, plus known asset extensions.
  case "$1" in
    *.png|*.jpg|*.jpeg|*.gif|*.ico|*.pdf|*.zip|*.dll|*.exe|*.so|*.dylib|*.woff|*.woff2|*.ttf) return 1 ;;
  esac
  # `grep -Iq` returns non-zero for binary files.
  grep -Iq . "$1" 2>/dev/null
}

# ---------------------------------------------------------------- pass 1: content
echo "--- pass 1: file content ---"
CONTENT_FILES=0
while IFS= read -r -d '' f; do
  is_text "$f" || continue
  grep -q -e 'Nexo' -e 'NEXO' -e 'nexo' "$f" 2>/dev/null || continue
  CONTENT_FILES=$((CONTENT_FILES + 1))
  if [[ $APPLY -eq 1 ]]; then
    sed -i -e 's/Nexo/Ashlar/g' -e 's/NEXO/ASHLAR/g' -e 's/nexo/ashlar/g' "$f"
  fi
done < <(find . \( "${PRUNE[@]}" \) -prune -o -type f -print0)
echo "files with Nexo/NEXO/nexo in content: $CONTENT_FILES"
echo

# ---------------------------------------------------------------- pass 2: filenames
echo "--- pass 2: file names ---"
FILE_RENAMES=0
while IFS= read -r -d '' f; do
  base="$(basename "$f")"; dir="$(dirname "$f")"
  new="${base//Nexo/Ashlar}"; new="${new//NEXO/ASHLAR}"; new="${new//nexo/ashlar}"
  [[ "$new" == "$base" ]] && continue
  FILE_RENAMES=$((FILE_RENAMES + 1))
  [[ $APPLY -eq 0 ]] && { echo "  $f  ->  $dir/$new"; continue; }
  mv "$f" "$dir/$new"
done < <(find . \( "${PRUNE[@]}" \) -prune -o -type f -iname '*nexo*' -print0)
echo "files renamed: $FILE_RENAMES"
echo

# ---------------------------------------------------------------- pass 3: directories
# Deepest first (-depth), so renaming a parent never invalidates a queued child path.
echo "--- pass 3: directory names (deepest first) ---"
DIR_RENAMES=0
while IFS= read -r -d '' d; do
  base="$(basename "$d")"; parent="$(dirname "$d")"
  new="${base//Nexo/Ashlar}"; new="${new//NEXO/ASHLAR}"; new="${new//nexo/ashlar}"
  [[ "$new" == "$base" ]] && continue
  DIR_RENAMES=$((DIR_RENAMES + 1))
  [[ $APPLY -eq 0 ]] && { echo "  $d  ->  $parent/$new"; continue; }
  mv "$d" "$parent/$new"
done < <(find . -depth \( "${PRUNE[@]}" \) -prune -o -type d -iname '*nexo*' -print0)
echo "directories renamed: $DIR_RENAMES"
echo

if [[ $APPLY -eq 1 ]]; then
  echo "=== done. Next: ==="
  echo "  bash scripts/handoff/verify-rename.sh"
  echo "  dotnet build Ashlar.Kernel.sln"
  echo "  git add -A && git commit"
else
  echo "=== dry run complete. Re-run with --apply to execute. ==="
fi
