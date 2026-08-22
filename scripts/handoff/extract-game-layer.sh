#!/usr/bin/env bash
# Extract the game-specific surface out of the kernel into a standalone tree at
# _handoff/game-layer/, ready to become its own repository consuming Ashlar as a
# package.
#
#   bash scripts/handoff/extract-game-layer.sh            # report
#   bash scripts/handoff/extract-game-layer.sh --apply    # move the files
#
# RUN THIS BEFORE rename-to-ashlar.sh. The paths below are Nexo-named; running the
# rename first invalidates every one of them. (rename-to-ashlar.sh skips _handoff/
# on purpose, so the extracted tree keeps its own identity.)
#
# ---------------------------------------------------------------------------
# WHAT THIS SCRIPT DOES AND DOES NOT DO
#
# TIER 1 — mechanical, and all this script touches.
#   Self-contained game code with no inbound references from kernel production
#   code. Verified: `AddPlaytestServices` has ZERO callers anywhere in the tree,
#   and nothing outside Nexo.Orchestration references the Playtest namespaces.
#   Moving these cannot break a kernel build.
#
# TIER 2 — needs a compiler in the loop. NOT done here.
#   src/Nexo.Orchestration/Assets/ and Agents/Assets/ (generative image/audio/3D).
#   No inbound references from outside Orchestration, so the move itself is clean,
#   but AgentFactory constructs these types, so it must be cut over at the same
#   time as Tier 3. Move them together with Tier 3, not separately.
#
# TIER 3 — a real refactor, NOT a file move. NOT done here.
#   src/Nexo.Orchestration/Agents/Templates/{Combat,Economy,Gameplay,AI}Agent.cs
#   and src/Nexo.Orchestration/Architect/DomainRecognizer.cs.
#
#   AgentFactory.CreateAgent switches on hardcoded domain strings ("combat",
#   "economy", "ai", "gameplay") and news up the concrete type. DomainRecognizer
#   hardcodes regex patterns for the same domains plus ammo/loot/NPC/pathfinding.
#   Extracting these requires turning both into registries the game package fills:
#
#     - IDomainAgentProvider   { bool Handles(string domain); BaseAgent Create(spec); }
#     - IDomainPatternProvider { IReadOnlyDictionary<string, IReadOnlyList<Regex>> Patterns { get; } }
#
#   AgentFactory then resolves IEnumerable<IDomainAgentProvider> and asks each in
#   turn; DomainRecognizer composes its pattern table from the registered providers.
#   InfrastructureAgent and SecurityAgent are NOT game-specific — they stay in the
#   kernel and register as providers like everything else.
#
#   Do this on a machine with `dotnet build`. It is roughly a day of work and it
#   is the only part of the extraction that can break the kernel.
# ---------------------------------------------------------------------------

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

DEST="_handoff/game-layer"
APPLY=0
[[ "${1:-}" == "--apply" ]] && APPLY=1

# Tier 1: source directories with no inbound kernel references, as "source|destination".
# Destinations are spelled out rather than derived from the basename: both Playtest
# directories share a basename and would otherwise collide into one folder.
TIER1_DIRS=(
  "src/Nexo.Orchestration/Playtest|Playtest"
  "src/Nexo.Orchestration/Agents/Playtest|Agents/Playtest"
)
TIER1_FILES=(
  "src/Nexo.Tools.Dev/TileMapRenderTool.cs"
)
# Tests that exercise only Tier 1 and move with it.
TIER1_TESTS=(
  "src/Nexo.Tests.Orchestration/OrchestrationPlaytestCoordinationTests.cs"
  "src/Nexo.Tests.Orchestration/OrchestrationSecurityPlaytestTests.cs"
)

if [[ $APPLY -eq 0 ]]; then
  echo "=== DRY RUN — nothing will be moved. Pass --apply to execute. ==="
else
  echo "=== EXTRACTING game layer -> $DEST ==="
fi
echo

echo "--- Tier 1: directories ---"
for entry in "${TIER1_DIRS[@]}"; do
  d="${entry%%|*}"; sub="${entry##*|}"
  if [[ ! -d "$d" ]]; then echo "  SKIP (absent): $d"; continue; fi
  n=$(find "$d" -type f | wc -l | tr -d ' ')
  echo "  $d  ($n files)  ->  $DEST/src/GameLayer/$sub"
  if [[ $APPLY -eq 1 ]]; then
    mkdir -p "$DEST/src/GameLayer/$(dirname "$sub")"
    mv "$d" "$DEST/src/GameLayer/$sub"
  fi
done
echo

echo "--- Tier 1: individual files ---"
for f in "${TIER1_FILES[@]}"; do
  if [[ ! -f "$f" ]]; then echo "  SKIP (absent): $f"; continue; fi
  echo "  $f  ->  $DEST/src/GameLayer/Tools/$(basename "$f")"
  if [[ $APPLY -eq 1 ]]; then
    mkdir -p "$DEST/src/GameLayer/Tools"
    mv "$f" "$DEST/src/GameLayer/Tools/$(basename "$f")"
  fi
done
echo

echo "--- Tier 1: tests ---"
for f in "${TIER1_TESTS[@]}"; do
  if [[ ! -f "$f" ]]; then echo "  SKIP (absent): $f"; continue; fi
  echo "  $f  ->  $DEST/tests/GameLayer.Tests/$(basename "$f")"
  if [[ $APPLY -eq 1 ]]; then
    mkdir -p "$DEST/tests/GameLayer.Tests"
    mv "$f" "$DEST/tests/GameLayer.Tests/$(basename "$f")"
  fi
done
echo

if [[ $APPLY -eq 1 ]]; then
  cat > "$DEST/README.md" <<'MD'
# Game layer (extracted from the Ashlar kernel)

Tier 1 of the game extraction: self-contained game code that had **no inbound
references** from kernel production code. `AddPlaytestServices` had zero callers
anywhere in the tree, so removing this from the kernel cannot break a kernel build.

## Name

`GameLayer` is a **placeholder**. Pick the real product name, then:

    grep -rl GameLayer . | xargs sed -i 's/GameLayer/YourName/g'
    find . -depth -iname '*GameLayer*' -exec bash -c 'mv "$0" "${0//GameLayer/YourName}"' {} \;

## Making this a repository

1. Pick the name (above).
2. `git init && git add -A && git commit -m "chore: extract game layer from Ashlar kernel"`
3. Add a `nuget.config` and `Directory.Packages.props` — copy them from
   `consumer-template/` in the Ashlar repo, which exists for exactly this.
4. Reference the kernel packages. **Until Ashlar publishes to nuget.org there is
   nothing to restore**, so until then use one of:
   - a local folder feed packed from an Ashlar checkout, or
   - a `ProjectReference` into a sibling Ashlar checkout (simplest while iterating).

## Still in the kernel

Tier 2 (asset generation) and Tier 3 (domain agent templates, DomainRecognizer)
were deliberately left behind — Tier 3 needs `AgentFactory` and `DomainRecognizer`
turned into registries first. See the header of `scripts/handoff/extract-game-layer.sh`
in the Ashlar repo for the interfaces to introduce.
MD

  echo "=== done. Next: ==="
  echo "  1. Remove AddPlaytestServices from src/Nexo.Orchestration/ServiceCollectionExtensions.cs (~line 157)"
  echo "  2. Drop the moved files from Nexo.Orchestration.csproj / Nexo.Tools.Dev.csproj if they are listed explicitly"
  echo "  3. dotnet build Nexo.Kernel.sln    # must still be green"
  echo "  4. Then run scripts/handoff/rename-to-ashlar.sh"
else
  echo "=== dry run complete. Re-run with --apply to execute. ==="
fi
