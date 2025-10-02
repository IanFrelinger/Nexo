# NexoDirectorStudio — Productized Pipeline

## Surfaces
- **Editor:** Nexo → Director Runbook (prompt, seed, artifacts; run phases; open artifacts).
- **CLI:** `scripts/run-with-config.sh` (uses `nexo.pipeline.json`).
- **CI:** `scripts/ci-verify.sh` (UTF PlayMode preferred → smoke fallback with JUnit).

## Config
- Root `nexo.pipeline.json` drives prompt, seed, artifacts, phase order, acceptance, and scenarios.
- `nexo.adapters.json` holds adapter endpoints; keep secrets out of Git.

## Artifacts
- `Artifacts/<runId>/<Phase>/output.json` per phase + `run.summary.json`.
- Smoke emits `playmode-smoke.json` and `playmode-smoke.junit.xml`. On failure, a `.png` screenshot.

## Determinism
- Seed flows through RunContext; same prompt + seed ⇒ same world.

## Extensibility
- Implement new phases via `IPhase<TIn,TOut>` and register with `PhaseRegistry`.
- Adapters implement your existing interfaces; add health checks and timeouts.
