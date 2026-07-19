#!/usr/bin/env bash
# Extract a proprietary C++ parser and draft an IEventReader adapter into the
# Poco/evtx service as common/adapted_reader.hpp.
#
# Default path: Nexo DepExtract GUI (CppDependencyExtractorBrick +
# CppParserAdapterBrick) at $NEXO_DEPEXTRACT_GUI (default http://localhost:5237).
# Extract-only path: Docker image `dep-extract` (--skip-adapt / --manifest-only).
#
# Prerequisites: Docker + Ollama code model + .NET SDK for the GUI path.
# Prefer WSL with trees on ext4 (not /mnt/c).
#
# Usage:
#   # terminal A
#   dotnet run --project src/Nexo.Bricks.DepExtract.Gui
#   # terminal B
#   bash scripts/adapt-parser-to-poco.sh \
#     --src /path/to/proprietary/tree \
#     --entry relative/Parser.cpp \
#     --poco /path/to/Poco \
#     [--entry another.hpp] [--model codellama:7b] [--include-dir inc] \
#     [--define KEY=VAL] [--scan-dir .] [--manifest-only] [--skip-adapt]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC=""
POCO=""
MODEL="codellama:7b"
SCAN_DIR="."
MANIFEST_ONLY=0
SKIP_ADAPT=0
ENTRIES=()
INCLUDE_DIRS=()
DEFINES=()
GUI_URL="${NEXO_DEPEXTRACT_GUI:-http://localhost:5237}"

while [ $# -gt 0 ]; do
  case "$1" in
    --src)           SRC="$2"; shift 2 ;;
    --poco)          POCO="$2"; shift 2 ;;
    --entry)         ENTRIES+=("$2"); shift 2 ;;
    --model)         MODEL="$2"; shift 2 ;;
    --scan-dir)      SCAN_DIR="$2"; shift 2 ;;
    --include-dir)   INCLUDE_DIRS+=("$2"); shift 2 ;;
    --define)        DEFINES+=("$2"); shift 2 ;;
    --manifest-only) MANIFEST_ONLY=1; SKIP_ADAPT=1; shift ;;
    --skip-adapt)    SKIP_ADAPT=1; shift ;;
    --gui-url)       GUI_URL="$2"; shift 2 ;;
    -h|--help)
      sed -n '2,22p' "$0" | sed 's/^# \{0,1\}//'
      exit 0
      ;;
    *) echo "unknown argument: $1" >&2; exit 2 ;;
  esac
done

die() { echo "error: $*" >&2; exit 1; }

[ -n "$SRC" ]  || die "--src is required"
[ -n "$POCO" ] || die "--poco is required (path to the Poco/evtx checkout)"
[ "${#ENTRIES[@]}" -gt 0 ] || die "at least one --entry is required"
[ -d "$SRC" ]  || die "src not found: $SRC"
[ -d "$POCO" ] || die "poco root not found: $POCO"
[ -f "$POCO/poco/main.cpp" ] || die "missing poco/main.cpp — not the Poco/evtx tree: $POCO"

command -v docker >/dev/null || die "docker not found — run scripts/setup-dep-extract-wsl.sh"
command -v python3 >/dev/null || die "python3 not found"

if ! docker image inspect dep-extract >/dev/null 2>&1; then
  echo "== building dep-extract image =="
  docker build -t dep-extract "$ROOT/tools/dep_extract"
fi

KEEP="$POCO/build/nexo-last-extract"
mkdir -p "$KEEP"

run_dep_extract() {
  local out="$1"
  mkdir -p "$out"
  local args=(run --rm --network=none
    -v "$SRC:/src:ro"
    -v "$out:/out"
    dep-extract)
  local e d
  for e in "${ENTRIES[@]}"; do args+=(--entry "$e"); done
  args+=(--scan-dir "$SCAN_DIR")
  for d in "${INCLUDE_DIRS[@]}"; do args+=(--include-dir "$d"); done
  for d in "${DEFINES[@]}"; do args+=(--define "$d"); done
  if [ "$MANIFEST_ONLY" -eq 1 ]; then args+=(--manifest-only); fi
  echo "== dep-extract → $out =="
  docker "${args[@]}"
}

if [ "$SKIP_ADAPT" -eq 1 ]; then
  run_dep_extract "$KEEP"
  echo "extract-only artifacts → $KEEP"
  [ -f "$KEEP/manifest.txt" ] && sed -n '1,40p' "$KEEP/manifest.txt"
  exit 0
fi

command -v curl >/dev/null || die "curl not found"

echo "== adapt via DepExtract GUI at $GUI_URL =="
if ! curl -sf "$GUI_URL/api/status" >/dev/null; then
  die "GUI not reachable at $GUI_URL — start it with: dotnet run --project $ROOT/src/Nexo.Bricks.DepExtract.Gui"
fi

ENTRIES_JSON=$(printf '%s\n' "${ENTRIES[@]}" | python3 -c 'import json,sys; print(json.dumps([l.strip() for l in sys.stdin if l.strip()]))')
INCLUDES_JSON=$(printf '%s\n' "${INCLUDE_DIRS[@]}" | python3 -c 'import json,sys; print(json.dumps([l.strip() for l in sys.stdin if l.strip()]))')
DEFINES_JSON=$(printf '%s\n' "${DEFINES[@]}" | python3 -c 'import json,sys; print(json.dumps([l.strip() for l in sys.stdin if l.strip()]))')

PAYLOAD=$(SRC_JSON=$(python3 -c 'import json,sys; print(json.dumps(sys.argv[1]))' "$SRC") \
MODEL_JSON=$(python3 -c 'import json,sys; print(json.dumps(sys.argv[1]))' "$MODEL") \
SCAN_JSON=$(python3 -c 'import json,sys; print(json.dumps(sys.argv[1]))' "$SCAN_DIR") \
python3 - <<PY
import json
print(json.dumps({
  "srcDir": json.loads('''$SRC_JSON'''),
  "entries": json.loads('''$ENTRIES_JSON'''),
  "scanDir": json.loads('''$SCAN_JSON'''),
  "includeDirs": json.loads('''$INCLUDES_JSON'''),
  "defines": json.loads('''$DEFINES_JSON'''),
  "includeUpper": False,
  "manifestOnly": False,
  "model": json.loads('''$MODEL_JSON'''),
  "maxRetries": 3,
}))
PY
)

RESP="$KEEP/adapt-response.json"
HTTP_CODE=$(curl -sS -o "$RESP" -w "%{http_code}" \
  -X POST "$GUI_URL/api/run" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD" || true)

if [ "$HTTP_CODE" != "200" ]; then
  echo "GUI adapt failed (HTTP ${HTTP_CODE:-000})" >&2
  [ -f "$RESP" ] && cat "$RESP" >&2
  echo "Falling back to extract-only into $KEEP" >&2
  run_dep_extract "$KEEP"
  exit 1
fi

python3 - <<PY
import json, pathlib, shutil, sys
r = json.load(open(r"$RESP", encoding="utf-8"))
code = r.get("adapterCode") or ""
if not code.strip():
    print("error: GUI returned empty adapterCode", file=sys.stderr)
    sys.exit(1)
out = pathlib.Path(r"$POCO") / "common" / "adapted_reader.hpp"
out.write_text(code if code.endswith("\n") else code + "\n", encoding="utf-8")
print(f"installed adapter → {out}")
print(f"  sourceApiCoverage     = {r.get('sourceApiCoverage')}")
print(f"  looksLikeTemplateEcho = {r.get('looksLikeTemplateEcho')}")
hall = r.get("possibleHallucinations") or []
if hall:
    print(f"  possibleHallucinations= {', '.join(hall)}")
dup = r.get("duplicatePath")
if dup and pathlib.Path(dup).is_dir():
    dest = pathlib.Path(r"$KEEP") / "duplicate"
    if dest.exists():
        shutil.rmtree(dest)
    shutil.copytree(dup, dest)
    print(f"  duplicate copied       → {dest}")
manifest = r.get("manifestText") or ""
if manifest:
    (pathlib.Path(r"$KEEP") / "manifest.txt").write_text(manifest, encoding="utf-8")
print(r.get("summary") or "")
PY

echo
echo "Next:"
echo "  1. Review $POCO/common/adapted_reader.hpp"
echo "  2. g++ -std=c++20 -O2 -DNEXO_USE_CUSTOM -Icommon poco/main.cpp -o /tmp/evtx \\"
echo "       -lPocoNet -lPocoUtil -lPocoFoundation -lpthread"
echo "  3. bash $POCO/tools/no_gui_gate.sh /tmp/evtx && bash $POCO/tests/run.sh"
