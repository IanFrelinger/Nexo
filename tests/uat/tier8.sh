#!/usr/bin/env bash
# Nexo UAT — Tier 8 (documentation truthfulness, swept across every page).
#
# Two of the defects this suite found were doc rot: a command that a build change quietly broke (#350)
# and a path the docs called gitignored that was tracked (#354). Both were found by reading one page
# closely. This tier does mechanically, across every page, what cannot be done by reading: it checks
# that the things the documentation NAMES still exist, and that the commands it PRINTS can still run.
#
# It deliberately does not execute arbitrary documented commands -- that would be slow and unsafe. It
# checks the properties that rot silently while a command still looks correct on the page.
set -uo pipefail

SRC="${UAT_REPO_DIR:-$PWD}"
OUT="${UAT_OUT:-${WORK:-/work}/out}"
mkdir -p "$OUT"
: >"$OUT/results-tier8.tsv"

PASS=0; FAIL=0
result() {
  local tier="$1" id="$2" verdict="$3"; shift 3
  printf 'RESULT\t%s\t%s\t%s\t%s\n' "$tier" "$id" "$verdict" "$*" | tee -a "$OUT/results-tier8.tsv"
  if [ "$verdict" = PASS ]; then PASS=$((PASS+1)); else FAIL=$((FAIL+1)); fi
}
say() { printf '\n=== %s ===\n' "$*"; }
cd "$SRC" || exit 1

DOCS=$(find docs README.md CONTRIBUTING.md SECURITY.md deploy samples consumer-template -name '*.md' 2>/dev/null | sort)
DOC_COUNT=$(printf '%s\n' "$DOCS" | grep -c .)

say "8.1 every repo path a FOLLOW-ALONG page names still exists"
# Scoped on purpose. Swept across all ~$DOC_COUNT pages this produced ~85 hits and zero real defects,
# because documentation legitimately names paths that do not exist:
#   - plans and inventories (FleetGovernanceExtractionInventory.md, *-spec.md, *-manifest.md) name
#     paths they intend to CREATE; that is what a plan is
#   - pages name paths the reader creates (deploy/compose/.env, deploy/compose/local/) or a run
#     generates (spikes/**/generated/) -- all gitignored, which is the rule used to skip them
#   - CONTRIBUTING names `src/Nexo.CLI` in the NEGATIVE, to warn that the CLI is not there
# A check that cries wolf gets muted, so this asks the narrower question that actually matters: on the
# pages a tester or operator is told to FOLLOW, does every path they are pointed at exist?
FOLLOW_ALONG="docs/TesterQuickstart.md docs/GettingStarted.md docs/IntegratorGuide.md docs/DEPLOYMENT.md
docs/AuthoringBricks.md docs/Configuration.md docs/CopilotMvpWalkthrough.md docs/SelfHostedAgentServer.md
README.md deploy/compose/README.md samples/hello-brick/README.md consumer-template/CONSUMING.md"
MISSING=""; CHECKED=0; PAGES=0; SKIPPED_IGNORED=0
for doc in $FOLLOW_ALONG; do
  [ -f "$doc" ] || { MISSING="$MISSING [page absent: $doc]"; continue; }
  PAGES=$((PAGES+1))
  for p in $(grep -oE '`(src|application|docs|scripts|deploy|samples|spikes|tests|consumer-template|commercial|\.docker|\.github|\.devcontainer)/[A-Za-z0-9._/*-]+`' "$doc" \
             | tr -d '`' | sort -u); do
    case "$p" in *\**) continue;; esac                 # globs are patterns, not paths
    [ -e "$p" ] && { CHECKED=$((CHECKED+1)); continue; }
    # A path the reader creates or a run generates is gitignored by design, not missing.
    if git check-ignore -q "$p" 2>/dev/null; then SKIPPED_IGNORED=$((SKIPPED_IGNORED+1)); continue; fi
    MISSING="$MISSING $(basename "$doc"):$p"
  done
done
if [ -z "$MISSING" ]; then
  result 8 doc-paths-exist PASS "$CHECKED shipped paths across $PAGES follow-along pages resolve ($SKIPPED_IGNORED reader-created/gitignored paths skipped)"
else
  result 8 doc-paths-exist FAIL "named but absent:$MISSING"
fi

say "8.2 every documented 'dotnet run --project X' can actually run"
# This is #350 generalised. A project that multi-targets cannot be started without -f/--framework:
# dotnet refuses and prints "Your project targets multiple frameworks". The command still LOOKS right
# on the page, which is why a human reread never caught it and a build change silently broke it.
BADCMD=""; CMDS=0
while IFS= read -r line; do
  proj=$(printf '%s' "$line" | grep -oE -- '--project +[A-Za-z0-9._/-]+' | awk '{print $2}')
  [ -n "$proj" ] || continue
  CMDS=$((CMDS+1))
  csproj=""
  if [ -f "$proj" ]; then csproj="$proj"
  else csproj=$(find "$proj" -maxdepth 1 -name '*.csproj' 2>/dev/null | head -1); fi
  if [ -z "$csproj" ]; then
    BADCMD="$BADCMD [$proj: no project]"
    continue
  fi
  if grep -q '<TargetFrameworks>' "$csproj" 2>/dev/null; then
    printf '%s' "$line" | grep -qE -- '(-f|--framework) +net' \
      || BADCMD="$BADCMD [$proj multi-targets but the command omits -f]"
  fi
done <<EOF
$(grep -rhoE 'dotnet run --project [^`|]*' $DOCS 2>/dev/null | sort -u)
EOF
if [ -z "$BADCMD" ]; then
  result 8 dotnet-run-commands PASS "$CMDS documented 'dotnet run --project' commands name a real project and select a framework where one is required"
else
  result 8 dotnet-run-commands FAIL "unrunnable as printed:$BADCMD"
fi

say "8.3 every documented 'docker compose -f <file>' names a file that exists"
BADC=""; CC=0
for f in $(grep -rhoE 'docker compose (-f|--file) +[A-Za-z0-9._/-]+\.ya?ml' $DOCS 2>/dev/null \
           | awk '{print $NF}' | sort -u); do
  CC=$((CC+1)); [ -e "$f" ] || BADC="$BADC $f"
done
if [ -z "$BADC" ]; then
  result 8 compose-paths-exist PASS "$CC documented compose files all exist"
else
  result 8 compose-paths-exist FAIL "named but absent:$BADC"
fi

say "8.4 every documented script invocation names a script that exists"
BADS=""; SC=0
for s in $(grep -rhoE '(bash|pwsh|powershell -NoProfile -ExecutionPolicy Bypass -File|\./) *(scripts|spikes)/[A-Za-z0-9._/-]+\.(sh|ps1)' $DOCS 2>/dev/null \
           | grep -oE '(scripts|spikes)/[A-Za-z0-9._/-]+\.(sh|ps1)' | sort -u); do
  SC=$((SC+1)); [ -e "$s" ] || BADS="$BADS $s"
done
if [ -z "$BADS" ]; then
  result 8 doc-scripts-exist PASS "$SC documented script paths all exist"
else
  result 8 doc-scripts-exist FAIL "named but absent:$BADS"
fi

say "summary"
printf 'PASS=%d FAIL=%d\n' "$PASS" "$FAIL" | tee "$OUT/summary-tier8.txt"
[ "$FAIL" -eq 0 ]
