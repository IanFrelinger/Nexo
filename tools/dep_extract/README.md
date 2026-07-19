# dep-extract

A standalone containerized utility that computes the `#include` dependency
closure of one or more C/C++ files, using the real compiler's dependency
scanner (`g++ -MM -MG`) rather than text-matching heuristics. Not specific to
evtx — run it against any C/C++ source tree.

**Nothing you mount is ever transmitted anywhere.** It runs entirely against
a bind-mounted volume on your own machine; there is no network access from
inside the container and no reason to add any. This exists specifically so
you can produce a standalone **duplicate** of a proprietary parser — your
source tree is only ever read, never modified — without pasting or
uploading any of its source.

## What it computes

- **`lower`** — everything your entry file(s) need, transitively, to
  compile. This is what you bring along if you're lifting the parser out as
  a standalone unit.
- **`upper`** — everything else in the scanned tree that transitively
  includes your entry file(s) — i.e. who would be affected if you moved,
  renamed, or changed the interface of the parser. Computed correctly across
  multiple hops (`Main.cpp` includes `Facade.hpp` includes `Parser.hpp`) in
  one pass, not just direct includers.
- **`external`** — things the entry references (via `#include`) that resolve
  to a path *outside* the mounted source directory. You'll need to mount
  those too (or a parent directory that contains them) for the closure to be
  complete.

## Build

```
docker build -t dep-extract tools/dep_extract
```

## Run

```
docker run --rm \
  -v /absolute/path/to/your/source:/src:ro \
  -v /absolute/path/to/output:/out \
  dep-extract \
  --entry path/relative/to/src/YourParser.cpp \
  --entry path/relative/to/src/YourParser.hpp
```

- Mount your source **read-only** (`:ro`) — the tool never needs to write to
  it, only `/out`.
- `--entry` is repeatable — pass every file that's genuinely part of "the
  parser" if it's split across several translation units.
- By default this physically copies the entry files plus their `lower`
  closure into `$OUT_DIR/duplicate`, preserving relative directory
  structure — a real standalone duplicate, ready to zip/tar and hand off.
  Pass `--manifest-only` to skip the copy and just get `manifest.txt` (a
  report) if you want to see the dependency list before committing to it.

## Options

| Flag | Default | Purpose |
|---|---|---|
| `--include-dir PATH` | — | extra `-I` root (relative to `/src`), repeatable — needed if your headers use `#include <foo/bar.h>` against a non-default include root |
| `--scan-dir PATH` | whole `/src` | limits the reverse (`upper`) search to a subtree — narrow this on a large tree for speed and relevance |
| `--std STANDARD` | `c++20` | passed to the compiler |
| `--compiler g++\|clang++` | `g++` | which compiler drives the scan |
| `--define KEY=VAL` | — | `-D`, repeatable — matters if your `#include`s are gated behind `#ifdef`s |
| `--extra-flag FLAG` | — | passed through to the compiler verbatim, repeatable |
| `--jobs N` | `nproc` | parallel `-MM` invocations during the reverse scan |
| `--include-upper` | off | also copy the `upper` closure into the duplicate (default: entry+lower only) |
| `--manifest-only` | off | skip producing `$OUT_DIR/duplicate` — just the dependency report |
| `--quiet` | off | suppress progress lines (manifest is still printed/written) |

If your entry point relies on conditional compilation (`#ifdef PLATFORM_X`)
to pick which headers get included, pass the `--define`s that match your
real build — otherwise the compiler's dependency scan will follow whichever
branch is unconditionally taken by default, same as it would for an actual
build without those defines.

## Output

Written to `$OUT_DIR` (the `/out` mount):
- `manifest.txt` — human-readable report (also printed to stdout)
- `entry.txt`, `lower.txt`, `upper.txt`, `external.txt` — one path per line,
  relative to `/src` (or `../`-prefixed for `external.txt`)
- `duplicate/` — the standalone copy (entry + lower, or also upper with
  `--include-upper`) — unless `--manifest-only` was passed

## Verifying it works

`tools/dep_extract/test/fixtures/` has four small synthetic projects with
known-correct dependency graphs, and `test/run_test.sh` is an automated
regression suite (18 checks) that runs the tool against all of them:

```
bash tools/dep_extract/test/run_test.sh
```

- **`basic`** — a parser, a facade wrapping it two hops away, plus decoy
  files that must *not* show up in the results (unrelated code, and a sibling
  file that shares a dependency but doesn't itself depend on the entry).
- **`diamond`** — two headers both include the same common ancestor; proves
  the shared file is deduped once, not counted twice.
- **`conditional`** — an `#ifdef`-gated include; proves `--define` actually
  changes which branch the scanner follows.
- **`deepchain`** — a 5-level include chain; proves `upper` transitivity
  holds past the 2-hop case `basic` already covers.

This is what to run after modifying `extract_deps.sh` — there's no way to
test changes against real proprietary code from this side, so the fixtures
are the correctness bar.

## Poco / IEventReader adaptation

After extraction, Nexo's `CppParserAdapterBrick` (DepExtract GUI) drafts a
headless `IEventReader` adapter for the Poco/evtx service. End-to-end:

```
bash scripts/setup-dep-extract-wsl.sh
dotnet run --project src/Nexo.Bricks.DepExtract.Gui   # http://localhost:5237
bash scripts/adapt-parser-to-poco.sh \
  --src /path/to/proprietary/tree \
  --entry relative/YourParser.cpp \
  --poco /path/to/Poco
```

That installs `common/adapted_reader.hpp` into the Poco tree; build the
server with `-DNEXO_USE_CUSTOM`. See the Poco checkout's `NEXO_INTEGRATION.md`
and `PARSER_SEAM.md`.

## Known limitations

- Dependency resolution follows whatever `#ifdef` branches are taken by
  default; pass `--define` to match your real build's macros if that matters.
- Path handling assumes no spaces in file/directory names (a long-standing
  limitation of Makefile-style `-M`/`-MM` output itself, not specific to this
  tool).
- `upper` is computed by scanning every source/header file under
  `--scan-dir` with its own `-MM` pass — fine for a subsystem or a
  medium-sized repo, but budget for it on a very large monorepo (narrow
  `--scan-dir`, or raise `--jobs`).
