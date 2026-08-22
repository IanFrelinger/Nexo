#!/usr/bin/env bash
# Extract the game-specific surface out of the kernel into a standalone tree at
# _handoff/game-layer/, ready to become its own repository consuming Ashlar as a
# package.
#
#   bash scripts/handoff/extract-game-layer.sh            # report + blocker check
#   bash scripts/handoff/extract-game-layer.sh --apply    # move the files
#
# ---------------------------------------------------------------------------
# READ THIS FIRST — THE ORIGINAL "TIER 1" CLASSIFICATION WAS WRONG.
#
# An earlier revision of this script claimed the moves below were "Tier 1:
# self-contained game code with no inbound references from kernel production
# code ... moving these cannot break a kernel build."
#
# That was measured and it is false. Applying it and running the documented
# `dotnet build Nexo.Kernel.sln` produces:
#
#   Build FAILED. 14 Error(s)
#
#   AgentFactory.cs(7,33):  CS0234 namespace 'Playtest' does not exist in 'Nexo.Orchestration.Agents'
#   AgentFactory.cs(12,26): CS0234 namespace 'Playtest' does not exist in 'Nexo.Orchestration'
#   ToolsDevTests.cs:       CS0246 'TileMapRenderTool' could not be found  (x10)
#
# The coupling is the SAME hardcoded-switch pattern this file already documented
# for Tier 3. AgentFactory.CreateAgent calls IsPlaytestDomain(spec.Domain) then
# CreatePlaytestAgent(spec), which switches on "aiplayer"/"playtest"/"balance"/
# "feedback" and news up AIPlayerAgent, BalanceAnalyzerAgent and
# FeedbackSynthesizerAgent, resolving IGameRunner and ITelemetryStore from
# Nexo.Orchestration.Playtest.Ports.
#
# Playtest is therefore TIER 3, not Tier 1. It needs IDomainAgentProvider first.
#
# A second reference does NOT appear in the Kernel.sln build and is easy to miss:
# RepoFsToolboxFactory.cs:48 and :95 do `tools.Register(new TileMapRenderTool())`.
# Nexo.BackgroundAgents.HostRunners is not a member of Nexo.Kernel.sln, so the
# handoff's own green-check cannot see it. Building that project directly against
# an applied extraction also fails.
#
# Net result: TIER 1 IS EMPTY. Every candidate has an inbound reference from code
# that stays behind. This script now refuses --apply until the blockers are gone.
#
# Still true from the original analysis:
#   - AddPlaytestServices has zero callers, and its body is entirely commented
#     out — an empty no-op that returns `services`. Deleting it is safe.
#
# Note on verification: you cannot fall back to `dotnet build Nexo.sln` to catch
# the wider breakage, because Nexo.sln does not restore on this branch at all —
#   GameDirector.Host.csproj : error NU1201: Project Nexo.API is not compatible
#   with net8.0. Project Nexo.API supports: net10.0
# That failure predates this work and is unrelated to it. Until it is fixed,
# verify by building the affected projects directly (see the bottom of this file).
# ---------------------------------------------------------------------------
#
# TIER 2 — needs a compiler in the loop. NOT done here.
#   src/Nexo.Orchestration/Assets/ and Agents/Assets/ (generative image/audio/3D).
#   AgentFactory constructs these types too, so cut them over with Tier 3.
#
# TIER 3 — a real refactor, NOT a file move. NOT done here.
#   Agents/Templates/{Combat,Economy,Gameplay,AI}Agent.cs, Architect/DomainRecognizer.cs,
#   AND the Playtest tree demoted from Tier 1 above.
#
#     - IDomainAgentProvider   { bool Handles(string domain); BaseAgent Create(spec); }
#     - IDomainPatternProvider { IReadOnlyDictionary<string, IReadOnlyList<Regex>> Patterns { get; } }
#
#   AgentFactory resolves IEnumerable<IDomainAgentProvider> and asks each in turn;
#   DomainRecognizer composes its pattern table from the registered providers.
#   InfrastructureAgent and SecurityAgent are NOT game-specific — they stay in the
#   kernel and register as providers like everything else.
#
#   TileMapRenderTool needs the same treatment one level down: RepoFsToolboxFactory
#   must take its tool set from a registry rather than newing up concrete tools.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

DEST="_handoff/game-layer"
APPLY=0
[[ "${1:-}" == "--apply" ]] && APPLY=1

# The kernel prefix is "Nexo" before the rename and "Ashlar" after it. Derive it at
# runtime rather than hardcoding, because this script lives under scripts/handoff/
# and rename-to-ashlar.sh deliberately does NOT rewrite that directory — the rename
# tooling must not rewrite itself (a first attempt did, and left verify-rename.sh
# grepping for the wrong token). So this cannot rely on being rewritten, and instead
# works unchanged on both sides of the rename.
# NB: no `head -1` here. Under `set -o pipefail`, head exiting early sends SIGPIPE
# to git ls-files and the whole script dies with 141 before printing anything.
# `sed -n` consumes all input, so nothing gets a broken pipe.
KP="$(git ls-files 'src/*.Orchestration/*' | sed -n '1{s|^src/||; s|\.Orchestration/.*||; p;}')"
[[ -n "$KP" ]] || { echo "could not derive kernel prefix from src/*.Orchestration" >&2; exit 1; }
echo "kernel prefix: $KP"
echo

# source|destination. Destinations are spelled out rather than derived from the
# basename: both Playtest directories share a basename and would otherwise
# collide into one folder.
MOVE_DIRS=(
  "src/$KP.Orchestration/Playtest|Playtest"
  "src/$KP.Orchestration/Agents/Playtest|Agents/Playtest"
  # Game-domain recognition patterns (Combat, Economy, Gameplay, game-AI), lifted out of
  # DomainRecognizer's hardcoded table via IDomainPatternProvider.
  "src/$KP.Orchestration/GameDomain|GameDomain"
)
MOVE_FILES=(
  "src/$KP.Tools.Dev/TileMapRenderTool.cs"
)
MOVE_TESTS=(
  "src/$KP.Tests.Orchestration/OrchestrationPlaytestCoordinationTests.cs"
  "src/$KP.Tests.Orchestration/OrchestrationSecurityPlaytestTests.cs"
  # TileMapRenderTool coverage. The first was split out of ToolsDevTests, where 10
  # tile tests were interleaved with 40 unrelated kernel-tool tests and kept
  # Ashlar.Tests.Kernel referencing the type.
  "src/$KP.Tests.Kernel/TileMapRenderToolCoverageTests.cs"
  "src/$KP.Tests.BackgroundAgents/Tools/TileMapRenderToolTests.cs"
  # Game-domain pattern coverage; names GameDomainPatternProvider so it moves with it.
  "src/$KP.Tests.Orchestration/GameDomainPatternTests.cs"
)

# --------------------------------------------------------------- blocker check
# Anything that stays behind and still names what we are about to move.
echo "--- inbound-reference check ---"
BLOCKERS=0

# Regex-escape a literal path. '.' is the only regex metacharacter that occurs in
# these paths, and leaving it unescaped would let "Tools.Dev" match "ToolsXDev".
esc() { printf '%s' "$1" | sed 's/\./\\./g'; }

staying() {
  # Tracked .cs files that are NOT themselves being moved.
  #
  # Derived from the MOVE_* arrays rather than restated. An earlier version listed
  # the exclusions by hand, which is a standing invitation for the two lists to
  # drift: add a file to MOVE_TESTS, forget the matching grep, and the blocker
  # check reports the file you are about to move as a blocker against itself.
  local pat="" e
  for e in "${MOVE_DIRS[@]}"; do pat="${pat}|^$(esc "${e%%|*}")/"; done
  for e in "${MOVE_FILES[@]}" "${MOVE_TESTS[@]}"; do pat="${pat}|^$(esc "$e")\$"; done
  git ls-files "*.cs" | grep -vE "${pat#|}"
}

for sym in "${KP}\.Orchestration\.Agents\.Playtest" \
           "${KP}\.Orchestration\.Playtest" \
           "${KP}\.Orchestration\.GameDomain" \
           "TileMapRenderTool"; do
  plain="${sym//\\/}"
  hits="$(staying | tr "\n" "\0" | xargs -0 grep -ln "$sym" 2>/dev/null || true)"
  if [[ -n "$hits" ]]; then
    printf "  BLOCKED  %s still referenced by:\n" "$plain"
    while IFS= read -r h; do [[ -n "$h" ]] && printf "             %s\n" "$h"; done <<< "$hits"
    BLOCKERS=$((BLOCKERS + 1))
  else
    printf "  ok       %s — no inbound references\n" "$plain"
  fi
done
echo

if [[ $BLOCKERS -gt 0 ]]; then
  echo "=== $BLOCKERS blocker(s). Moving now would break the build. ==="
  echo "Introduce IDomainAgentProvider / IDomainPatternProvider and a tool registry"
  echo "first (see the header of this file), then re-run."
  if [[ $APPLY -eq 1 ]]; then
    echo "refusing --apply." >&2
    exit 1
  fi
  echo "(dry run — reporting the move plan anyway)"
  echo
fi

if [[ $APPLY -eq 0 ]]; then
  echo "=== DRY RUN — nothing will be moved. Pass --apply to execute. ==="
else
  echo "=== EXTRACTING game layer -> $DEST ==="
fi
echo

echo "--- directories ---"
for entry in "${MOVE_DIRS[@]}"; do
  d="${entry%%|*}"; sub="${entry##*|}"
  if [[ ! -d "$d" ]]; then echo "  SKIP (absent): $d"; continue; fi
  n=$(find "$d" -type f | wc -l | tr -d " ")
  echo "  $d  ($n files)  ->  $DEST/src/GameLayer/$sub"
  if [[ $APPLY -eq 1 ]]; then
    mkdir -p "$DEST/src/GameLayer/$(dirname "$sub")"
    mv "$d" "$DEST/src/GameLayer/$sub"
  fi
done
echo

echo "--- individual files ---"
for f in "${MOVE_FILES[@]}"; do
  if [[ ! -f "$f" ]]; then echo "  SKIP (absent): $f"; continue; fi
  echo "  $f  ->  $DEST/src/GameLayer/Tools/$(basename "$f")"
  if [[ $APPLY -eq 1 ]]; then
    mkdir -p "$DEST/src/GameLayer/Tools"
    mv "$f" "$DEST/src/GameLayer/Tools/$(basename "$f")"
  fi
done
echo

echo "--- tests ---"
for f in "${MOVE_TESTS[@]}"; do
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

`GameLayer` is a **placeholder** name. Pick the real one, then:

    grep -rl GameLayer . | xargs sed -i 's/GameLayer/YourName/g'
    find . -depth -iname '*GameLayer*' -exec bash -c 'mv "$0" "${0//GameLayer/YourName}"' {} \;

## Making this a repository

1. Pick the name (above).
2. `git init && git add -A && git commit -m "chore: extract game layer from Ashlar kernel"`
3. Copy `nuget.config` and `Directory.Packages.props` from `consumer-template/`
   in the Ashlar repo, which exists for exactly this.
4. **Nothing is published to nuget.org and there are no git tags**, so there is no
   package to restore yet. Until then use a local folder feed packed from an Ashlar
   checkout, or a `ProjectReference` into a sibling checkout (simplest while iterating).
MD

  echo "=== done. Next: ==="
  echo "  1. Remove AddPlaytestServices from src/${KP}.Orchestration/ServiceCollectionExtensions.cs"
  echo "     (empty no-op, zero callers — safe deletion)"
  echo "  2. Drop the moved files from any .csproj that lists them explicitly"
  echo "  3. Verify. ${KP}.sln does not restore (NU1201, pre-existing), so build the"
  echo "     affected projects directly:"
  echo "       dotnet build ${KP}.Kernel.sln"
  echo "       dotnet build src/${KP}.BackgroundAgents.HostRunners/${KP}.BackgroundAgents.HostRunners.csproj"
  echo "       dotnet build src/${KP}.Tests.BackgroundAgents/${KP}.Tests.BackgroundAgents.csproj"
else
  echo "=== dry run complete. ==="
fi
