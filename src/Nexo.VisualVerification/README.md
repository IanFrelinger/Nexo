# Nexo Visual Verification (Phase 0)

Engine-agnostic visual verification harness for deterministic scripted scenarios. The harness runs a target application under a virtual display, captures frames each step, and emits a content-bound **eye-test session record** signed with Ed25519.

## What this attests

- **Presentation stability** for a given `scenario hash + build artifact hash + display configuration` under pixel-tier checks.
- **Determinism** via mandatory double-run gate: identical frame hashes across two consecutive runs.
- **Integrity** of the certified record and referenced frame bytes via `RecordSha256` and Ed25519 signature.

## What this does not attest

- Correctness of gameplay, simulation logic, or scene semantics.
- GPU rendering fidelity (Phase 0 is software-only).
- Semantic or VLM-based visual judgment.
- Telemetry or scene-graph equivalence.

## Threat model

| Threat | Mitigation |
|--------|------------|
| Tampered frame bytes after capture | Frame SHA-256 checked against record on verification |
| Tampered record fields | `RecordSha256` + Ed25519 signature binding |
| Wrong scenario/build paired with golden | Golden comparison binds scenario + build hashes |
| Hollow capture (blank/constant frames) | `BlankFrameDetector` rejects identical frames when script expects change |
| Golden leakage into target providers | Golden store types isolated; source-scan rejection test |
| Nondeterministic target | Double-run gate → `NonDeterministic`; signing rejected |

## Architecture

```
src/Nexo.VisualVerification/            # records, hashing, signing, golden store
src/Nexo.VisualVerification.Providers/  # display + target provider seams, harness
tools/Nexo.ToyRenderer/                 # deterministic toy target
tests/Nexo.VisualVerification.Tests/    # rejection + unit tests
```

Provider seams speak only in terms of `scenario hash + build artifact hash + input script + frame stream`. No engine-specific types exist in the core library.

## Determinism prerequisites for future target providers

1. Seeded PRNG with explicit algorithm (never default `System.Random`).
2. Fixed timestep simulation; one harness step equals one logical tick.
3. Scripted inputs delivered only at declared steps.
4. Software rendering path available for CI (no GPU/EGL requirement).
5. No wall-clock, thread-id, or environment-derived state in the render path.

## Perceptual hash

Phase 0 uses **dHash** (9×8 grayscale difference hash, 64-bit). Default Hamming-distance epsilon is **5**, pinned by rejection test `R7_PerceptualEpsilonBoundary`.

## Record shape decision

`EyeTestSessionRecord` is a **parallel sibling** to `CertifiedTransition`, not a subtype. `CertifiedTransition` models hash-chained state-log entries; eye-test records bind a single capture session with frame artifacts and pixel-tier verdicts. Both reuse Ed25519 via NSec with canonical JSON payloads.

## Frame storage

Frames are **hash-referenced**: raw RGBA8888 bytes stored at `{frameStorageRoot}/{sha256}.raw`. The session record carries SHA-256 and perceptual hashes plus storage paths.

## Running tests

```bash
dotnet test tests/Nexo.VisualVerification.Tests/Nexo.VisualVerification.Tests.csproj
```

CI path uses `SimulatedDisplayProvider` only — no Xvfb, GPU, or display server required.

Xvfb integration tests are guarded:

```bash
# Skipped automatically when Xvfb is unavailable
dotnet test --filter "Category=XvfbIntegration"
```

## Demo

```bash
dotnet run --project tools/Nexo.ToyRenderer/Nexo.ToyRenderer.csproj -- /tmp/nexo-eye-demo
```

The demo generates a golden record, re-verifies a clean run, and demonstrates tampered signature failure.

## Open questions resolved in this sprint

| Question | Decision |
|----------|----------|
| `CertifiedTransition` subtype? | Parallel `EyeTestSessionRecord` with same Ed25519 authority |
| Perceptual hash + epsilon | dHash, epsilon = 5, boundary test pinned |
| Frame embedding vs reference | Hash-referenced raw storage |
