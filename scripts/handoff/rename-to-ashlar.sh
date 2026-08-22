#!/usr/bin/env bash
# Rename Nexo -> Ashlar across the repository.
#
# Dry run by default. Nothing is written unless you pass --apply.
#
#   bash scripts/handoff/rename-to-ashlar.sh            # report what would change
#   bash scripts/handoff/rename-to-ashlar.sh --apply    # do it
#
# SCOPE IS DRIVEN BY `git ls-files`, NOT `find`.
#
# This matters. An earlier revision of this script walked the tree with `find` and
# pruned only ./.git and ./_handoff. On a working checkout that also swept up:
#
#   - .claude/worktrees/<name>/   a full 1.5 GB git worktree, 4,429 matching files.
#     It is listed in .git/info/exclude, so `git status` never shows it and
#     `git checkout .` / `git reset --hard` CANNOT restore it. The pass-1 sed also
#     rewrites that worktree's `.git` gitfile (it contains the absolute path
#     ".../Nexo-Framework/.git/worktrees/..."), permanently breaking the link.
#   - bin/ and obj/               3,896 matching files of build output.
#
# 11,854 files instead of the intended 4,000. Tracked files are exactly the set
# that needs renaming; build output regenerates and worktrees are separate
# checkouts that must be renamed on their own branch, if at all.
#
# The script is IDEMPOTENT: after the first pass there are no `Nexo` tokens left.
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
# "nexo" as a substring — every hit is a compound identifier such as AddNexo or
# INexoClient. Plain token replacement is safe and needs no word-boundary guard.
#
# NOT covered, because git does not track them — handle by hand:
#   .env files, local shell profiles, CI secrets, and the .nexo/ state directory.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

git rev-parse --git-dir >/dev/null 2>&1 || { echo "not a git repository: $REPO_ROOT" >&2; exit 1; }

APPLY=0
[[ "${1:-}" == "--apply" ]] && APPLY=1

if [[ $APPLY -eq 0 ]]; then
  echo "=== DRY RUN — nothing will be written. Pass --apply to execute. ==="
else
  echo "=== APPLYING rename Nexo -> Ashlar in $REPO_ROOT ==="
  # Tracked modifications only. Untracked files do not pollute the rename diff, and
  # counting them makes this guard environment-dependent: the host filters .claude/
  # via a global excludes file that root inside the dev container does not have, so
  # `git status --porcelain` is clean on the host and dirty in the container for the
  # same tree.
  if [[ -n "$(git status --porcelain --untracked-files=no)" ]]; then
    echo "tracked files have uncommitted changes — commit or stash first, so this rename is reviewable alone." >&2
    git status --porcelain --untracked-files=no >&2
    exit 1
  fi
fi
echo

# Paths excluded from the rename, and why:
#
#   _handoff/         the extracted game layer keeps its own identity (see
#                     extract-game-layer.sh). Untracked once the extraction
#                     creates it, so git ls-files already skips it; belt-and-braces
#                     for the case where it has been committed.
#
#   scripts/handoff/  THE RENAME'S OWN TOOLING. This is not cosmetic — leaving it
#   docs/handoff/     in the scope actively destroys it. A first attempt renamed
#                     these too, and verify-rename.sh had its own search tokens
#                     rewritten: it came out grepping for "Ashlar" and announcing
#                     "verifying rename Ashlar -> Ashlar", so it reported the
#                     entire correctly-renamed tree as residue. The docs fared no
#                     better — "Full rename. Nexo -> Ashlar" became
#                     "Full rename. Ashlar -> Ashlar", and every sentence
#                     explaining what was renamed lost its meaning.
#
#                     These files are ABOUT the rename. Their Nexo tokens are the
#                     definition of the work, not instances of it.
#
#                     extract-game-layer.sh used to depend on being rewritten,
#                     because it hardcoded src/Nexo.Orchestration/... paths. It now
#                     derives the prefix at runtime instead, so it works unchanged
#                     before and after.
tracked() {
  git ls-files -z | grep -zZv -e '^_handoff/' -e '^scripts/handoff/' -e '^docs/handoff/'
}

is_text() {
  case "$1" in
    *.png|*.jpg|*.jpeg|*.gif|*.ico|*.pdf|*.zip|*.dll|*.exe|*.so|*.dylib|*.woff|*.woff2|*.ttf) return 1 ;;
  esac
  grep -Iq . "$1" 2>/dev/null
}

# ---------------------------------------------------------------- pass 1: content
echo "--- pass 1: file content ---"
CONTENT_FILES=0
while IFS= read -r -d '' f; do
  [[ -f "$f" ]] || continue
  is_text "$f" || continue
  grep -q -e 'Nexo' -e 'NEXO' -e 'nexo' "$f" 2>/dev/null || continue
  CONTENT_FILES=$((CONTENT_FILES + 1))
  [[ $APPLY -eq 1 ]] && sed -i -e 's/Nexo/Ashlar/g' -e 's/NEXO/ASHLAR/g' -e 's/nexo/ashlar/g' "$f"
done < <(tracked)
echo "files with Nexo/NEXO/nexo in content: $CONTENT_FILES"
echo

# ---------------------------------------------------------------- pass 2: filenames
echo "--- pass 2: file names ---"
FILE_RENAMES=0
while IFS= read -r -d '' f; do
  [[ -f "$f" ]] || continue
  base="$(basename "$f")"; dir="$(dirname "$f")"
  new="${base//Nexo/Ashlar}"; new="${new//NEXO/ASHLAR}"; new="${new//nexo/ashlar}"
  [[ "$new" == "$base" ]] && continue
  FILE_RENAMES=$((FILE_RENAMES + 1))
  [[ $APPLY -eq 0 ]] && { echo "  $f  ->  $dir/$new"; continue; }
  git mv -- "$f" "$dir/$new"
done < <(tracked)
echo "files renamed: $FILE_RENAMES"
echo

# ---------------------------------------------------------------- pass 3: directories
# Derived from tracked paths only, deepest first, so renaming a parent never
# invalidates a queued child path.
echo "--- pass 3: directory names (deepest first) ---"
DIR_RENAMES=0
while IFS= read -r d; do
  [[ -d "$d" ]] || continue
  base="$(basename "$d")"; parent="$(dirname "$d")"
  new="${base//Nexo/Ashlar}"; new="${new//NEXO/ASHLAR}"; new="${new//nexo/ashlar}"
  [[ "$new" == "$base" ]] && continue
  DIR_RENAMES=$((DIR_RENAMES + 1))
  [[ $APPLY -eq 0 ]] && { echo "  $d  ->  $parent/$new"; continue; }
  mv -- "$d" "$parent/$new"
done < <(git ls-files | grep -v '^_handoff/' \
          | awk -F/ '{p=""; for(i=1;i<NF;i++){p=(i==1?$1:p"/"$i); print p}}' \
          | sort -u \
          | awk -F/ '{print NF"\t"$0}' | sort -rn -k1,1 | cut -f2-)
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
