#!/usr/bin/env bash
# extract_deps.sh — produce a standalone DUPLICATE of one or more entry files
# plus everything they need to compile, using the real compiler's -MM
# dependency scanner (g++/clang++ -MM -MG) rather than text-matching
# heuristics. Runs entirely against a mounted source tree ($SRC_DIR); nothing
# is transmitted anywhere, and the source tree itself is only ever read, never
# written — this is meant to run against code that never leaves the machine
# it's built on.
#
# LOWER  = everything the entry file(s) need, transitively, to compile
#          (found by running -MM on the entry itself).
# UPPER  = everything under --scan-dir that transitively includes any entry
#          file (found by running -MM on every file in the scan tree and
#          checking whether an entry file shows up in its closure — this
#          naturally catches multi-hop chains, e.g. Main.cpp -> Facade.hpp ->
#          Parser.hpp, without a separate BFS pass, since -MM already
#          flattens transitivity for us).
#
# By default, entry+lower are physically copied into $OUT_DIR/duplicate
# (preserving relative structure) — a real standalone duplicate, not just a
# report. Pass --manifest-only to skip that and only get the dependency lists.
set -uo pipefail

SRC_DIR="${SRC_DIR:-/src}"
OUT_DIR="${OUT_DIR:-/out}"
COMPILER="${COMPILER:-g++}"
STD="${STD:-c++20}"
SCAN_DIR="."
JOBS="$(nproc 2>/dev/null || echo 4)"
DUPLICATE=1
INCLUDE_UPPER=0
QUIET=0
ENTRIES=()
INCLUDE_DIRS=()
DEFINES=()
EXTRA_FLAGS=()
SRC_EXTS="cpp cc cxx C"
HDR_EXTS="h hpp hh hxx H"

usage() {
  cat <<'EOF'
extract_deps.sh — compiler-driven #include dependency closure extractor

Required:
  --entry PATH             file (relative to $SRC_DIR) that defines "the unit"
                            to extract. Repeatable — pass every one of its own
                            .cpp/.hpp files if it's split across several.

Optional:
  --include-dir PATH       extra -I include root (relative to $SRC_DIR), repeatable
  --scan-dir PATH          subtree (relative to $SRC_DIR) to scan for reverse
                            ("upper") dependents. Default: whole $SRC_DIR.
  --std STANDARD           C++ standard passed to the compiler (default: c++20)
  --compiler g++|clang++   which compiler drives dependency scanning (default: g++)
  --define KEY=VAL         -D passed to the compiler, repeatable
  --extra-flag FLAG        passed through to the compiler verbatim, repeatable
                            (e.g. --extra-flag -DPLATFORM_LINUX)
  --jobs N                 parallel -MM invocations for the reverse scan (default: nproc)
  --include-upper          also copy the upper closure into the duplicate (default: entry+lower only)
  --manifest-only          skip producing $OUT_DIR/duplicate — just the dependency report
  --quiet                  suppress per-file progress
  -h, --help                this help

By default this produces a standalone DUPLICATE of the entry file(s) and
everything they need to compile, physically copied into $OUT_DIR/duplicate
(preserving relative directory structure) — not just a dependency report.
Your mounted source tree is never modified; --manifest-only skips the copy
if you only want to see manifest.txt first.

Env: SRC_DIR (default /src), OUT_DIR (default /out) — mount points.

Example:
  docker run --rm -v /path/to/code:/src:ro -v /path/to/out:/out dep-extract \
    --entry parser/MyParser.cpp --entry parser/MyParser.hpp \
    --include-dir include --scan-dir .
EOF
}

while [ $# -gt 0 ]; do
  case "$1" in
    --entry)         ENTRIES+=("$2"); shift 2 ;;
    --include-dir)   INCLUDE_DIRS+=("$2"); shift 2 ;;
    --scan-dir)      SCAN_DIR="$2"; shift 2 ;;
    --std)           STD="$2"; shift 2 ;;
    --compiler)      COMPILER="$2"; shift 2 ;;
    --define)        DEFINES+=("$2"); shift 2 ;;
    --extra-flag)    EXTRA_FLAGS+=("$2"); shift 2 ;;
    --jobs)          JOBS="$2"; shift 2 ;;
    --manifest-only) DUPLICATE=0; shift ;;
    --include-upper) INCLUDE_UPPER=1; shift ;;
    --quiet)         QUIET=1; shift ;;
    -h|--help)       usage; exit 0 ;;
    *) echo "unknown argument: $1" >&2; usage >&2; exit 2 ;;
  esac
done

if [ "${#ENTRIES[@]}" -eq 0 ]; then echo "error: at least one --entry is required" >&2; usage >&2; exit 2; fi
if ! command -v "$COMPILER" >/dev/null 2>&1; then echo "error: compiler '$COMPILER' not found in this image" >&2; exit 2; fi
if [ ! -d "$SRC_DIR" ]; then echo "error: SRC_DIR '$SRC_DIR' not found (did you mount it?)" >&2; exit 2; fi
mkdir -p "$OUT_DIR"

log() { [ "$QUIET" -eq 1 ] || echo "$@" >&2; }

# --- build a single flag string (not an array) so it survives into background subshells cleanly ---
COMPILE_FLAGS="-std=$STD -I$SRC_DIR"
for d in "${INCLUDE_DIRS[@]:-}"; do [ -n "$d" ] && COMPILE_FLAGS="$COMPILE_FLAGS -I$SRC_DIR/$d"; done
for d in "${DEFINES[@]:-}"; do [ -n "$d" ] && COMPILE_FLAGS="$COMPILE_FLAGS -D$d"; done
for f in "${EXTRA_FLAGS[@]:-}"; do [ -n "$f" ] && COMPILE_FLAGS="$COMPILE_FLAGS $f"; done

norm() { # norm PATH -> canonical path relative to SRC_DIR (may start with .. if outside it)
  local p="$1"
  case "$p" in /*) : ;; *) p="$SRC_DIR/$p" ;; esac
  realpath -m --relative-to="$SRC_DIR" "$p" 2>/dev/null
}
is_source() {
  local ext="${1##*.}" e
  for e in $SRC_EXTS; do [ "$ext" = "$e" ] && return 0; done
  return 1
}

# closure_of PATH -> normalized dependency paths, one per line (excludes nothing; includes PATH itself)
closure_of() {
  local f="$1" abs mmout tmpcpp=""
  case "$f" in /*) abs="$f" ;; *) abs="$SRC_DIR/$f" ;; esac
  if is_source "$abs"; then
    mmout=$(cd "$SRC_DIR" && "$COMPILER" -MM -MG $COMPILE_FLAGS "$abs" 2>/dev/null)
  else
    tmpcpp=$(mktemp --suffix=.cpp)
    printf '#include "%s"\n' "$abs" > "$tmpcpp"
    mmout=$(cd "$SRC_DIR" && "$COMPILER" -MM -MG $COMPILE_FLAGS "$tmpcpp" 2>/dev/null)
    rm -f "$tmpcpp"
  fi
  [ -z "$mmout" ] && return 1
  printf '%s\n' "$mmout" \
    | sed 's/^[^:]*://; s/\\$//' \
    | tr -s ' \t' '\n' \
    | sed '/^$/d' \
    | while IFS= read -r tok; do
        [ -n "$tmpcpp" ] && [ "$tok" = "$tmpcpp" ] && continue   # drop our own synthetic wrapper
        norm "$tok"
      done
}

log "== resolving entry files =="
ENTRY_NORM=()
for e in "${ENTRIES[@]}"; do
  abs="$SRC_DIR/$e"
  [ -f "$abs" ] || { echo "error: entry not found: $e" >&2; exit 2; }
  ENTRY_NORM+=("$(norm "$abs")")
  log "  entry: $(norm "$abs")"
done
printf '%s\n' "${ENTRY_NORM[@]}" | sort -u > "$OUT_DIR/.entry.tmp"

log "== computing lower (forward) closure =="
: > "$OUT_DIR/.lower_raw.tmp"
: > "$OUT_DIR/.external.tmp"
for e in "${ENTRIES[@]}"; do
  closure_of "$e" >> "$OUT_DIR/.lower_raw.tmp" || log "  warning: -MM failed for entry $e (check --include-dir / --define)"
done
sort -u "$OUT_DIR/.lower_raw.tmp" -o "$OUT_DIR/.lower_raw.tmp"
grep -v '^\.\./' "$OUT_DIR/.lower_raw.tmp" > "$OUT_DIR/.lower_internal.tmp" || true
grep '^\.\./'    "$OUT_DIR/.lower_raw.tmp" > "$OUT_DIR/.external.tmp" || true
grep -vxFf "$OUT_DIR/.entry.tmp" "$OUT_DIR/.lower_internal.tmp" > "$OUT_DIR/lower.txt" 2>/dev/null || true
# ^ grep exits 1 whenever every line was excluded (all deps were entry files) — that's a valid
# empty result, not an error, so the fallback must not resurrect the unfiltered list.
sort -u "$OUT_DIR/.external.tmp" -o "$OUT_DIR/external.txt"

log "== scanning $SCAN_DIR for reverse (upper) dependents =="
mapfile -t SCAN_FILES < <(
  find "$SRC_DIR/$SCAN_DIR" -type f 2>/dev/null \
    | grep -E "\.($(echo "$SRC_EXTS $HDR_EXTS" | tr ' ' '|'))\$" \
    | sort
)
log "  ${#SCAN_FILES[@]} files to scan (jobs=$JOBS)"

TMPD=$(mktemp -d)
idx=0 running=0
for f in "${SCAN_FILES[@]}"; do
  rel=$(norm "$f")
  # skip entry files themselves — not useful as an "upper" candidate for themselves.
  # Exact whole-line match, not substring: "Parser.hpp" must not skip "OldParser.hpp".
  printf '%s\n' "${ENTRY_NORM[@]}" | grep -qxF "$rel" && continue
  idx=$((idx+1))
  ( closure_of "$rel" > "$TMPD/$idx.deps"; echo "$rel" > "$TMPD/$idx.name" ) &
  running=$((running+1))
  if [ "$running" -ge "$JOBS" ]; then wait -n 2>/dev/null || wait; running=$((running-1)); fi
done
wait

: > "$OUT_DIR/upper.txt"
for namefile in "$TMPD"/*.name; do
  [ -e "$namefile" ] || continue
  n="${namefile%.name}"
  candidate=$(cat "$namefile")
  depsfile="$n.deps"
  [ -s "$depsfile" ] || continue
  if grep -qxFf "$OUT_DIR/.entry.tmp" "$depsfile" 2>/dev/null; then
    echo "$candidate" >> "$OUT_DIR/upper.txt"
  fi
done
sort -u "$OUT_DIR/upper.txt" -o "$OUT_DIR/upper.txt" 2>/dev/null || true
rm -rf "$TMPD"

mv "$OUT_DIR/.entry.tmp" "$OUT_DIR/entry.txt"
rm -f "$OUT_DIR"/.*.tmp

n_lower=$(wc -l < "$OUT_DIR/lower.txt" | tr -d ' ')
n_upper=$(wc -l < "$OUT_DIR/upper.txt" | tr -d ' ')
n_ext=$(wc -l < "$OUT_DIR/external.txt" | tr -d ' ')

{
  echo "# dep_extract manifest"
  echo "# entry file(s):"
  sed 's/^/#   /' "$OUT_DIR/entry.txt"
  echo "#"
  echo "# LOWER — needed to compile the entry (bring these along): $n_lower files"
  sed 's/^/lower  /' "$OUT_DIR/lower.txt"
  echo "#"
  echo "# UPPER — files under $SCAN_DIR that (transitively) include the entry: $n_upper files"
  sed 's/^/upper  /' "$OUT_DIR/upper.txt"
  if [ "$n_ext" -gt 0 ]; then
    echo "#"
    echo "# EXTERNAL — referenced by the entry but outside \$SRC_DIR (mount them too, or ignore if optional/platform-specific): $n_ext"
    sed 's/^/external /' "$OUT_DIR/external.txt"
  fi
} > "$OUT_DIR/manifest.txt"

log "== summary =="
log "  lower (dependencies):     $n_lower"
log "  upper (dependents):       $n_upper"
log "  external (outside \$SRC_DIR): $n_ext"
log "  manifest: $OUT_DIR/manifest.txt"

if [ "$DUPLICATE" -eq 1 ]; then
  log "== writing standalone duplicate to $OUT_DIR/duplicate =="
  rm -rf "$OUT_DIR/duplicate"
  mkdir -p "$OUT_DIR/duplicate"
  copy_list() {
    while IFS= read -r rel; do
      [ -z "$rel" ] && continue
      mkdir -p "$OUT_DIR/duplicate/$(dirname "$rel")"
      cp -p "$SRC_DIR/$rel" "$OUT_DIR/duplicate/$rel"
    done
  }
  cat "$OUT_DIR/entry.txt" "$OUT_DIR/lower.txt" | copy_list
  [ "$INCLUDE_UPPER" -eq 1 ] && cat "$OUT_DIR/upper.txt" | copy_list
  log "  duplicated to $OUT_DIR/duplicate (source tree untouched)"
fi

cat "$OUT_DIR/manifest.txt"
