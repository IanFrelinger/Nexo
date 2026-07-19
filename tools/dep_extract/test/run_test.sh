#!/usr/bin/env bash
# tools/dep_extract/test/run_test.sh — regression suite for dep-extract,
# run against several synthetic fixtures with known-correct dependency
# graphs (test/fixtures/*). This is the correctness bar for the tool since
# we can't test it against real proprietary code — it never leaves the
# machine it's run on.
#
# fixtures/basic       parser -> Common/{Types,Utils}; Facade/Main wrap it
#                       2 hops away; Utils.cpp and Unrelated/* are decoys.
# fixtures/diamond      Top/Entry includes Left+Right, both of which include
#                       the same Common/Base — tests dedup of a diamond.
# fixtures/conditional  Core/Engine #ifdef-selects PlatformA or PlatformB —
#                       tests that --define actually changes the result.
# fixtures/deepchain    a 5-level include chain — tests upper transitivity
#                       goes past the 2-hop case basic/ already covers.
set -uo pipefail
cd "$(dirname "$0")/.."   # tools/dep_extract/

# Docker Desktop on Windows needs Windows-style absolute paths (C:/...) in
# `-v HOST:CONTAINER` — a plain MSYS `pwd` (/c/...) or a relative path gets
# mis-split on the colon. `pwd -W` (git-bash specific) gives the right form.
ROOT="$(pwd -W 2>/dev/null || pwd)"
FIXTURES="$ROOT/test/fixtures"
IMG=dep-extract
PASS=0; FAIL=0
ok()  { PASS=$((PASS+1)); echo "  [PASS] $1"; }
bad() { FAIL=$((FAIL+1)); echo "  [FAIL] $1"; [ -n "${2:-}" ] && echo "      x $2"; }
eq()  { if [ "$2" = "$3" ]; then ok "$1"; else bad "$1" "got [$2] want [$3]"; fi; }

run() {  # run FIXTURE OUTDIR ARG... -> prints manifest to stdout
  local fx="$1" out="$2"; shift 2
  rm -rf "$out"; mkdir -p "$out"
  docker run --rm -v "$FIXTURES/$fx:/src:ro" -v "$ROOT/$out:/out" "$IMG" --quiet "$@"
}
runsub() {  # runsub FIXTURE SUBDIR OUTDIR ARG... -> mounts fixtures/FIXTURE/SUBDIR as /src
  local fx="$1" sub="$2" out="$3"; shift 3
  rm -rf "$out"; mkdir -p "$out"
  docker run --rm -v "$FIXTURES/$fx/$sub:/src:ro" -v "$ROOT/$out:/out" "$IMG" --quiet "$@"
}
lines() { sort "$1" 2>/dev/null | tr '\n' ',' | sed 's/,$//'; }
findsorted() { find "$1" -type f | sed "s#^$1/##" | sort | tr '\n' ',' | sed 's/,$//'; }

echo "== building image =="
docker build -t "$IMG" -q . >/dev/null || { echo "docker build failed"; exit 1; }

echo "== T1 (basic): multi-file entry (Parser.cpp + Parser.hpp) =="
run basic test/out1 --entry parser/Parser.cpp --entry parser/Parser.hpp >/dev/null
eq "T1 lower = Types.hpp,Utils.hpp"        "$(lines test/out1/lower.txt)" "Common/Types.hpp,Common/Utils.hpp"
eq "T1 upper = Main,Facade.cpp,Facade.hpp" "$(lines test/out1/upper.txt)" "App/Main.cpp,Facade/Facade.cpp,Facade/Facade.hpp"

echo "== T2 (basic): header-only entry exercises the synthetic-wrapper path =="
run basic test/out2 --entry Facade/Facade.hpp >/dev/null
eq "T2 lower = Types.hpp,Parser.hpp" "$(lines test/out2/lower.txt)" "Common/Types.hpp,parser/Parser.hpp"
eq "T2 upper = Main.cpp,Facade.cpp"  "$(lines test/out2/upper.txt)" "App/Main.cpp,Facade/Facade.cpp"

echo "== T3 (basic): --scan-dir restricts reverse search to a subtree =="
run basic test/out3 --entry parser/Parser.hpp --scan-dir Facade >/dev/null
eq "T3 upper limited to Facade/*" "$(lines test/out3/upper.txt)" "Facade/Facade.cpp,Facade/Facade.hpp"

echo "== T4 (basic): duplicate is produced by default, --include-upper widens it =="
run basic test/out4 --entry parser/Parser.hpp --include-upper >/dev/null
eq "T4 duplicate = entry+lower+upper" "$(findsorted test/out4/duplicate)" \
  "App/Main.cpp,Common/Types.hpp,Facade/Facade.cpp,Facade/Facade.hpp,parser/Parser.cpp,parser/Parser.hpp"

echo "== T5 (basic): external dependency (mount only parser/, escaping include) =="
runsub basic parser test/out5 --entry Parser.hpp >/dev/null
eq "T5 lower is empty (Types.hpp is external, not internal)" "$(lines test/out5/lower.txt)" ""
eq "T5 external = ../Common/Types.hpp" "$(lines test/out5/external.txt)" "../Common/Types.hpp"
eq "T5 upper = Parser.cpp" "$(lines test/out5/upper.txt)" "Parser.cpp"

echo "== T6 (basic): unrelated files never appear anywhere =="
combined=$(cat test/out1/lower.txt test/out1/upper.txt)
case "$combined" in *Unrelated*) bad "T6 Unrelated/* excluded from T1 results" "$combined" ;; *) ok "T6 Unrelated/* excluded from T1 results" ;; esac
case "$combined" in *Utils.cpp*) bad "T6 Utils.cpp excluded (doesn't include Parser)" "$combined" ;; *) ok "T6 Utils.cpp excluded (doesn't include Parser)" ;; esac

echo "== T7 (basic): --manifest-only skips producing the duplicate =="
run basic test/out7 --entry parser/Parser.hpp --manifest-only >/dev/null
if [ -d test/out7/duplicate ]; then bad "T7 no duplicate/ dir created" "it exists"; else ok "T7 no duplicate/ dir created"; fi
if [ -f test/out7/manifest.txt ]; then ok "T7 manifest.txt still produced"; else bad "T7 manifest.txt still produced" "missing"; fi

echo "== T8 (diamond): shared common ancestor via two paths is deduped, not doubled =="
run diamond test/out8 --entry Top/Entry.hpp --entry Top/Entry.cpp >/dev/null
eq "T8 lower = Base.hpp,Left.hpp,Right.hpp (deduped)" "$(lines test/out8/lower.txt)" "Common/Base.hpp,Mid/Left.hpp,Mid/Right.hpp"
eq "T8 upper = Consumer/User.cpp" "$(lines test/out8/upper.txt)" "Consumer/User.cpp"

echo "== T9 (conditional): --define actually changes which branch is followed =="
run conditional test/out9a --entry Core/Engine.hpp --entry Core/Engine.cpp >/dev/null
eq "T9a default (no --define) resolves PlatformA" "$(lines test/out9a/lower.txt)" "PlatformA/Impl.hpp"
run conditional test/out9b --entry Core/Engine.hpp --entry Core/Engine.cpp --define USE_PLATFORM_B=1 >/dev/null
eq "T9b --define USE_PLATFORM_B=1 resolves PlatformB" "$(lines test/out9b/lower.txt)" "PlatformB/Impl.hpp"

echo "== T10 (deepchain): upper transitivity holds across 5 hops, not just 2 =="
run deepchain test/out10 --entry L0/Core.hpp >/dev/null
eq "T10 upper = all 5 levels of wrappers" "$(lines test/out10/upper.txt)" \
  "L1/Wrap1.hpp,L2/Wrap2.hpp,L3/Wrap3.hpp,L4/Wrap4.hpp,L5/Consumer.cpp"

rm -rf test/out1 test/out2 test/out3 test/out4 test/out5 test/out7 test/out8 test/out9a test/out9b test/out10

echo
echo "== $PASS/$((PASS+FAIL)) checks passed · $FAIL failures =="
if [ "$FAIL" -eq 0 ]; then echo "ALL TESTS GREEN"; exit 0; else echo "TESTS FAILING"; exit 1; fi
