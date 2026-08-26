# Test-depth audit — applications & apps layers (2026-08-26)

Prompted by "converged really fast": what do the fast green suites actually
prove? Measured with coverlet (cobertura) in a throwaway worktree of the agent
clone at `93741e61`, Release config, container toolchain — coverlet refs were
patched into the two test csprojs for the measurement only; nothing on the
branch changed. One run per suite; provenance measured on its unit slice
(`Category!=Integration`), matching what the readiness gate runs.

## Verdict in one paragraph

The suites are genuinely behavioral (crypto-backed resolution, rejection-path
assertions through fakes — not smoke), and the core libraries are well
covered: 80–92% line. The thinness is concentrated in three places with names:
the XREAL platform provider (31% — including a pure, native-free mapper at
0%), the certificate validation policy (55% of a security-relevant class),
and everything Neo4j in Provenance.Graph (0% — reachable only by the
integration slice, which needs a Docker daemon the dev container lacks).
The `apps` layer's checks assert CI's contract (markers, exit codes, files
scaffolded) and nothing deeper — bounded by CI's own thin definition.

## Measured coverage (line / branch / method)

Ashlar.Applications.Tests (94 tests, ~2 s):

| Assembly | Line | Branch | Method |
| --- | --- | --- | --- |
| Ashlar.Spatial.Runtime | 92.5% | 74.1% | 93.4% |
| Ashlar.Spatial.Contracts | 92.1% | 74.1% | 96.9% |
| Ashlar.Certification.Physical | 82.1% | 66.8% | 98.7% |
| Ashlar.Spatial.Multiplayer | 81.8% | 73.3% | 82.1% |
| Ashlar.Spatial.Platform.ARKit | 61.2% | 42.5% | 71.4% |
| Ashlar.Spatial.Platform.VisionPro | 58.3% | 41.3% | 64.1% |
| Ashlar.Spatial.Platform.XREAL | **31.3%** | 30.0% | 37.1% |
| **Total** | **79.3%** | **63.9%** | 84.8% |

Ashlar.Provenance.Graph.Tests, unit slice (15 tests):

| Assembly | Line | Branch | Method |
| --- | --- | --- | --- |
| Ashlar.Provenance.Graph | **49.9%** | 45.7% | 65.7% |

## Named gaps (classes ≥10 lines under 60%)

**Actionable now (pure unit, no native/external dependency):**
- `XrealTrackingStateMapper` — **0% of 36 lines.** A mapper with no native
  dependency; its ARKit/VisionPro counterparts are covered. The single
  cheapest coverage win in the layer.
- `PhysicalAtomCertificateValidationPolicy` — 54.7% of 106 lines. Half of a
  certificate **validation policy** is unexercised; security-relevant.
- `AssetContentHasher` — 42.9% (14 lines). Hashing helper.
- `GeoAnchorValidator` — 58.3% (48 lines).
- `XrealNativePoseFrame` (20%), `Uninitialized{ArKit,VisionPro,Xreal}NativeSession`
  (12–40%) — the fail-closed stubs; their rejection paths are small and
  cheaply assertable.

**Structurally blocked on the Docker lever (Testcontainers/Neo4j):**
- `Neo4jProvenanceGraphQueries` — `LineageOfAsync` (104 lines),
  `BlastRadiusOfAsync`, `ArtifactsUnderPolicyAsync`,
  `RequireFreshChainHeadAsync`: all 0%.
- `Neo4jProvenanceGraphStore.ProjectBatchAsync` (178 lines): 0%.
  These are exactly what the `applications-provenance-integration` tier-D
  lane runs; the unit slice cannot reach them meaningfully.

**Partially by design:** `Platform*NativeSession` interop (12–31%) is the
"headless hosts fail closed until native session wiring lands" seam — real
native paths are untestable in CI/container; only the fail-closed halves are
worth asserting.

## apps layer depth note

`scripts/apps-gate-checks.sh` mirrors CI exactly: banner/marker greps, exit
codes, scaffold files, a 2 s daemon window. It proves the script's CLI
contract, not runtime behavior — and three of four `apps/` surfaces have no
checks at all (parked product question in the ledger).

## Proposal — ratchet floors, kernel-gate style

Following `scripts/ci/kernel-coverage-gate.sh` philosophy (floor just under
measured; a RATCHET — may be raised, never lowered; never lowered to green a
build):

1. Reference `coverlet.msbuild` in both applications test csprojs (2 lines;
   central package management already pins 10.0.0).
2. New gate `applications-coverage` appended to the `applications` layer:
   one coverage run per suite + a checker that enforces per-assembly line
   floors from the cobertura output:
   Contracts **90** · Runtime **90** · Certification.Physical **80** ·
   Multiplayer **79** · ARKit **59** · VisionPro **56** · XREAL **29** ·
   Provenance.Graph **48** (unit slice).
   All floors pass today by construction; they stop erosion, not bless it.
3. Raise floors as tests land, in this order of value:
   XrealTrackingStateMapper tests (XREAL floor → ~55),
   PhysicalAtomCertificateValidationPolicy branch tests (Cert.Physical → 85),
   fail-closed assertions for the Uninitialized* sessions.
4. Pair the Docker lever with a Provenance ratchet: once
   `--include-tier-d` can run Neo4j integration locally, re-measure and lift
   the Provenance.Graph floor (expect ~75+).

Nothing was wired into the gate by this audit; the proposal awaits a human
go-ahead.

## Addendum — proposal executed (2026-08-26, same day)

All three levers landed, in the recommended order:

1. **Gate wired** (`eb745124`) and proven green at the measured baseline.
2. **Floors raised and earned through the fix pipeline** (`eac117fa` →
   fix `e6f5153a`): 73 behavioral tests targeting exactly the named gaps.
   Certification.Physical 82.1 → **91.6%** (floor now 90), XREAL 31.3 →
   **86.7%** (floor now 85); Contracts drifted up to 93.0. Full suite 167.
3. **Docker lever** (`30f51d78`, `c098cc9f`): container recreated from the
   branch devcontainer config with docker-outside-of-docker; Testcontainers
   needed Ryuk disabled + host-gateway override under DooD (first run
   failed in the reaper, fixed in the gate script). The Neo4j integration
   slice now runs locally: 4/4 green. **Provenance.Graph with integration:
   49.9 → 67.1% line** (56.4 branch) — the async query/store paths are no
   longer dark; remaining gap is mostly their error branches. The always-on
   coverage floor stays at 48 (unit slice, must pass without Docker); 67.1
   is the tier-D-verified number of record.
